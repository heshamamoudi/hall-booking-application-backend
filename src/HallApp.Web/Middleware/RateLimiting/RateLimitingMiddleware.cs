using System.Collections.Concurrent;
using System.Net;
using HallApp.Core.Exceptions;

namespace HallApp.Web.Middleware.RateLimiting;

/// <summary>
/// Unified rate limiting middleware using token bucket algorithm
/// Supports both global and endpoint-specific rate limiting
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;
    private static readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitingOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _options = options ?? new RateLimitingOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip rate limiting for excluded paths
        if (_options.ExcludedPaths.Any(p => path.StartsWith(p.ToLower())))
        {
            await _next(context);
            return;
        }

        // Skip rate limiting for localhost in development
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (_options.SkipLocalhost && IsLocalhost(clientIp))
        {
            await _next(context);
            return;
        }

        // Skip rate limiting for authenticated users if configured
        if (_options.SkipAuthenticated && context.User?.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Get client identifier
        string clientId = GetClientIdentifier(context, clientIp);

        // Get or create token bucket for this client
        var bucket = _buckets.GetOrAdd(clientId, _ => new TokenBucket(
            _options.MaxBucketSize,
            _options.TokensPerPeriod,
            TimeSpan.FromSeconds(_options.RefillIntervalSeconds)
        ));

        // Try to consume a token
        if (bucket.TryConsume(1))
        {
            // Add rate limit headers
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-RateLimit-Limit"] = _options.TokensPerPeriod.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = ((int)bucket.RemainingTokens).ToString();
                return Task.CompletedTask;
            });

            await _next(context);
        }
        else
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on path {Path}", clientId, path);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Retry-After"] = _options.RefillIntervalSeconds.ToString();

            var response = new ApiResponse(429, "Too many requests. Please try again later.");
            await context.Response.WriteAsJsonAsync(response);
        }
    }

    private string GetClientIdentifier(HttpContext context, string clientIp)
    {
        // Try to get API key from header if available
        var apiKey = context.Request.Headers["X-API-Key"].ToString();
        if (!string.IsNullOrEmpty(apiKey))
        {
            return $"api:{apiKey}";
        }

        // Use user ID if authenticated
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("uid")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }
        }

        return $"ip:{clientIp}";
    }

    private bool IsLocalhost(string ip)
    {
        return ip == "::1" || ip == "127.0.0.1" || ip == "::ffff:127.0.0.1";
    }

    /// <summary>
    /// Cleanup old buckets periodically (call from background service)
    /// </summary>
    public static void CleanupOldBuckets(TimeSpan maxAge)
    {
        var threshold = DateTime.UtcNow - maxAge;
        var keysToRemove = _buckets
            .Where(kvp => kvp.Value.RemainingTokens >= 100) // Full buckets are likely inactive
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _buckets.TryRemove(key, out _);
        }
    }
}

/// <summary>
/// Configuration options for rate limiting middleware
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Number of tokens added per refill period
    /// </summary>
    public int TokensPerPeriod { get; set; } = 100;

    /// <summary>
    /// Refill interval in seconds
    /// </summary>
    public int RefillIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum tokens that can be stored in the bucket
    /// </summary>
    public int MaxBucketSize { get; set; } = 100;

    /// <summary>
    /// Paths to exclude from rate limiting
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/swagger",
        "/health",
        "/favicon.ico"
    };

    /// <summary>
    /// Skip rate limiting for authenticated users
    /// </summary>
    public bool SkipAuthenticated { get; set; } = true;

    /// <summary>
    /// Skip rate limiting for localhost requests
    /// </summary>
    public bool SkipLocalhost { get; set; } = true;
}

/// <summary>
/// Extension methods for adding rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, RateLimitingOptions options)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>(options);
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, Action<RateLimitingOptions> configure)
    {
        var options = new RateLimitingOptions();
        configure(options);
        return builder.UseMiddleware<RateLimitingMiddleware>(options);
    }
}
