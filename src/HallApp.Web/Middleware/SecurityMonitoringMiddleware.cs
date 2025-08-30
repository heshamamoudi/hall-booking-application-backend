using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace HallApp.Web.Middleware;

/// <summary>
/// Middleware for monitoring and responding to suspicious security events
/// Implements OWASP A07:2021 (Identification and Authentication Failures) protection
/// </summary>
public class SecurityMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMonitoringMiddleware> _logger;
    private const string SECURITY_LOG_PREFIX = "[SECURITY_ALERT] ";
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, byte> _blockedIps = new();
    
    // SQL Injection patterns
    private static readonly Regex _sqlInjectionPattern = new Regex(
        @"(?:--\s*$)|(?:/\*.*?\*/)|(?:;[\s\n\r]*(?:drop|delete|update|insert)\s+)|(?:union[\s\n\r]*(?:all[\s\n\r]*)?select)|(?:exec[\s\n\r]*(?:xp|sp)_)|(?:';\s*shutdown)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
    // XSS patterns - comprehensive pattern to catch various XSS attacks
    private static readonly Regex _xssPattern = new Regex(
        @"(?:<script[\s\S]*?>)|(?:<\s*script\b)|(?:javascript\s*:)|(?:on(?:error|load|click|mouse|key|focus|blur|submit|change)[\s\n\r]*=)|(?:<iframe[\s\S]*?>)|(?:<svg[\s\S]*?on\w+[\s\n\r]*=)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
    // Path traversal patterns
    private static readonly Regex _pathTraversalPattern = new Regex(
        @"(?:\.\.[\\/])|(?:%2e%2e[\/\\])|(?:%252e%252e[\/\\])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
    // Protected endpoints that need extra scrutiny
    private readonly HashSet<string> _protectedEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/customer/login",
        "/api/token/refresh",
        "/api/vendors",
        "/api/vendors/type",
        "/api/vendor-types"
    };
    
    // CAPTCHA secret keys (in a real app, these would come from configuration)
    private const string CAPTCHA_SITE_KEY = "your-site-key"; // Public key for front-end
    private const string CAPTCHA_SECRET_KEY = "your-secret-key"; // Secret for verification
    
    public SecurityMonitoringMiddleware(RequestDelegate next, ILogger<SecurityMonitoringMiddleware> logger, 
        IMemoryCache cache, IConfiguration configuration)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = GetClientIpAddress(context);
        var requestPath = context.Request.Path.ToString().ToLowerInvariant();
        
        // Check if IP is blocked
        if (_blockedIps.ContainsKey(ipAddress))
        {
            _logger.LogWarning($"{SECURITY_LOG_PREFIX}Request from blocked IP: {ipAddress}");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync("Your IP has been blocked due to suspicious activity.");
            return;
        }
        
        // Enhanced security for all endpoints (not just authenticated ones)
        // Check for suspicious patterns in URL, query strings, and headers
        if (IsSuspiciousRequest(context))
        {
            IncrementSuspiciousActivityCount(ipAddress);
            _logger.LogWarning($"{SECURITY_LOG_PREFIX}Suspicious request detected from {ipAddress}, URL: {context.Request.Path}");
            
            // If suspicious activity exceeds threshold, block IP
            if (GetSuspiciousActivityCount(ipAddress) >= 5)
            {
                _blockedIps.TryAdd(ipAddress, 0);
                _logger.LogWarning($"{SECURITY_LOG_PREFIX}IP address blocked due to multiple suspicious activities: {ipAddress}");
            }
            
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync("Invalid request detected.");
            return;
        }
        
        // Special handling for vendor endpoints (both authenticated and unauthenticated)
        if (requestPath.StartsWith("/api/vendors") || requestPath.StartsWith("/api/vendor-types"))
        {
            // Monitor request frequency for API endpoints
            var endpointKey = $"endpoint:{requestPath}:{ipAddress}";
            var requestCount = _cache.GetOrCreate(endpointKey, entry => 
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });
            
            _cache.Set(endpointKey, requestCount + 1, TimeSpan.FromMinutes(1));
            
            // If excessive requests to vendor endpoints (more than 30 in 1 minute)
            if (requestCount > 30)
            {
                _logger.LogWarning($"{SECURITY_LOG_PREFIX}Excessive vendor endpoint requests from {ipAddress}: {requestCount} requests in 1 minute");
                
                // Special handling for vendor-specific endpoints to detect potential scraping
                context.Response.Headers["X-RateLimit-Limit"] = "30";
                context.Response.Headers["X-RateLimit-Remaining"] = "0";
                context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString();
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsync("Too many requests to vendor endpoints.");
                return;
            }
        }
        
        // For authentication endpoints, implement advanced monitoring and CAPTCHA
        if (IsAuthenticationEndpoint(context.Request.Path))
        {
            // Check if CAPTCHA is required based on previous failures
            if (RequiresCaptcha(ipAddress))
            {
                // Check for CAPTCHA token in request
                if (!context.Request.Headers.TryGetValue("X-CAPTCHA-TOKEN", out var captchaToken) ||
                    string.IsNullOrEmpty(captchaToken))
                {
                    _logger.LogInformation("CAPTCHA required but not provided for {IpAddress}", ipAddress);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync(JsonResponse(new 
                    { 
                        error = "CAPTCHA required",
                        captchaSiteKey = CAPTCHA_SITE_KEY,
                        requireCaptcha = true
                    }));
                    return;
                }
                
                // Verify CAPTCHA (in a real implementation, this would call reCAPTCHA API)
                // For demo purposes, any token with "valid" is accepted
                if (!captchaToken.ToString().Contains("valid"))
                {
                    _logger.LogWarning($"{SECURITY_LOG_PREFIX}Invalid CAPTCHA from {ipAddress}");
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync(JsonResponse(new 
                    { 
                        error = "Invalid CAPTCHA",
                        captchaSiteKey = CAPTCHA_SITE_KEY,
                        requireCaptcha = true
                    }));
                    return;
                }
                
                // Valid CAPTCHA, reset failed count
                _logger.LogInformation("Valid CAPTCHA from {IpAddress}, resetting failed login count", ipAddress);
                ResetFailedLoginAttempts(ipAddress);
            }
            
            // Capture the original body to analyze the response
            var originalBody = context.Response.Body;
            
            try
            {
                using var memStream = new MemoryStream();
                context.Response.Body = memStream;
                
                // Process the request
                await _next(context);
                
                // Check response status to identify failed login attempts
                if (context.Response.StatusCode == 401 || context.Response.StatusCode == 400)
                {
                    RecordFailedLoginAttempt(ipAddress);
                    var failedAttempts = GetFailedLoginAttempts(ipAddress);
                    
                    _logger.LogWarning($"{SECURITY_LOG_PREFIX}Failed login attempt {failedAttempts} for {ipAddress}");
                    
                    // Add header to indicate CAPTCHA requirement on next attempt
                    if (RequiresCaptcha(ipAddress))
                    {
                        context.Response.Headers["X-Require-Captcha"] = "true";
                        context.Response.Headers["X-Captcha-Site-Key"] = CAPTCHA_SITE_KEY;
                    }
                }
                
                // Copy the response to the original body
                memStream.Position = 0;
                await memStream.CopyToAsync(originalBody);
            }
            finally
            {
                context.Response.Body = originalBody;
            }
            
            return;
        }
        
        // Continue with the pipeline for normal requests
        await _next(context);
    }
    
    private bool IsSuspiciousRequest(HttpContext context)
    {
        // Get request components to check
        var path = context.Request.Path.ToString().ToLowerInvariant();
        var queryString = context.Request.QueryString.ToString().ToLowerInvariant();
        var fullUrl = $"{path}{queryString}".ToLowerInvariant();
        var headers = context.Request.Headers;
        
        // Log request for debugging
        _logger.LogDebug("Processing request: {FullUrl}", fullUrl);
        
        // More aggressive detection of malicious patterns
        // First check the full URL for any suspicious patterns
        var decodedUrl = Uri.UnescapeDataString(fullUrl);
        _logger.LogDebug("Checking decoded URL for malicious patterns: {DecodedUrl}", decodedUrl);
        
        // Check for SQL injection using more aggressive patterns
        if (_sqlInjectionPattern.IsMatch(decodedUrl))
        {
            _logger.LogWarning($"{SECURITY_LOG_PREFIX}SQL injection attempt detected in request: {path}{queryString}");
            return true;
        }
        
        // Check for XSS attempts with enhanced detection
        if (_xssPattern.IsMatch(decodedUrl))
        {
            _logger.LogWarning($"{SECURITY_LOG_PREFIX}XSS attempt detected in request: {decodedUrl}");
            return true;
        }
        
        // Check for path traversal with enhanced detection
        if (_pathTraversalPattern.IsMatch(decodedUrl))
        {
            _logger.LogWarning($"{SECURITY_LOG_PREFIX}Path traversal attempt detected in request: {decodedUrl}");
            return true;
        }
        
        // Also check query string parameters individually for better detection
        if (!string.IsNullOrEmpty(queryString) && queryString.Contains("="))
        {
            var queryParts = queryString.TrimStart('?').Split('&');
            foreach (var part in queryParts)
            {
                if (part.Contains("="))
                {
                    var value = part.Split('=')[1];
                    var decodedValue = Uri.UnescapeDataString(value);
                    
                    if (_sqlInjectionPattern.IsMatch(decodedValue) || _xssPattern.IsMatch(decodedValue) || _pathTraversalPattern.IsMatch(decodedValue))
                    {
                        _logger.LogWarning($"{SECURITY_LOG_PREFIX}Malicious pattern detected in query parameter: {decodedValue}");
                        return true;
                    }
                }
            }
        }
        
        // Check suspicious user agent
        if (headers.TryGetValue("User-Agent", out var userAgent))
        {
            var userAgentStr = userAgent.ToString().ToLowerInvariant();
            if (userAgentStr.Contains("sqlmap") || 
                userAgentStr.Contains("nikto") || 
                userAgentStr.Contains("burp") ||
                userAgentStr.Length < 5)
            {
                _logger.LogWarning($"{SECURITY_LOG_PREFIX}Suspicious user agent detected: {userAgentStr}");
                return true;
            }
        }
        
        return false;
    }
    
    private bool IsAuthenticationEndpoint(PathString path)
    {
        var pathStr = path.ToString().ToLowerInvariant();
        return pathStr == "/api/customer/login" || pathStr == "/api/token/refresh";
    }
    
    private void IncrementSuspiciousActivityCount(string ipAddress)
    {
        var cacheKey = $"suspicious_activity:{ipAddress}";
        var currentCount = _cache.GetOrCreate(cacheKey, entry => 
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            return 0;
        });
        
        _cache.Set(cacheKey, currentCount + 1, TimeSpan.FromHours(24));
    }
    
    private int GetSuspiciousActivityCount(string ipAddress)
    {
        var cacheKey = $"suspicious_activity:{ipAddress}";
        return _cache.GetOrCreate(cacheKey, entry => 
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            return 0;
        });
    }
    
    private void RecordFailedLoginAttempt(string ipAddress)
    {
        var cacheKey = $"failed_login:{ipAddress}";
        var currentCount = _cache.GetOrCreate(cacheKey, entry => 
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return 0;
        });
        
        _cache.Set(cacheKey, currentCount + 1, TimeSpan.FromHours(1));
    }
    
    private int GetFailedLoginAttempts(string ipAddress)
    {
        var cacheKey = $"failed_login:{ipAddress}";
        return _cache.GetOrCreate(cacheKey, entry => 
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return 0;
        });
    }
    
    private void ResetFailedLoginAttempts(string ipAddress)
    {
        var cacheKey = $"failed_login:{ipAddress}";
        _cache.Set(cacheKey, 0, TimeSpan.FromHours(1));
    }
    
    private bool RequiresCaptcha(string ipAddress)
    {
        // Require CAPTCHA after 3 failed attempts
        return GetFailedLoginAttempts(ipAddress) >= 3;
    }
    
    private string GetClientIpAddress(HttpContext context)
    {
        string ip = null!;
        
        // Try to get IP from forwarding headers first (for clients behind proxies)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedIp))
        {
            ip = forwardedIp.ToString().Split(',')[0].Trim();
        }
        
        // If no forwarded IP, use the connection remote IP
        if (string.IsNullOrEmpty(ip) && context.Connection.RemoteIpAddress != null)
        {
            ip = context.Connection.RemoteIpAddress.ToString();
        }
        
        return ip ?? "unknown";
    }
    
    private string JsonResponse(object data)
    {
        return JsonSerializer.Serialize(new 
        { 
            timestamp = DateTime.UtcNow,
            data = data
        });
    }
}
