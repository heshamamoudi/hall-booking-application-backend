using System.Collections.Concurrent;
using System.Net;
using HallApp.Web.Errors;
using Microsoft.Extensions.Logging;

namespace HallApp.Web.Middleware;

public class GlobalRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalRateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();

    // Global rate limit settings
    private const int TokensPerPeriod = 100; // Number of requests allowed in the period
    private const int RefillIntervalSeconds = 60; // Refill period in seconds
    private const int MaxBucketSize = 100; // Maximum number of tokens that can be stored

    public GlobalRateLimitingMiddleware(RequestDelegate next, ILogger<GlobalRateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for certain paths
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (
            path.StartsWith("/swagger") ||
            path.StartsWith("/health")))
        {
            await _next(context);
            return;
        }

        // Get client identifier (IP address)
        string clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Get or create token bucket for this client
        var bucket = _buckets.GetOrAdd(clientId, _ => new TokenBucket(
            MaxBucketSize,
            TokensPerPeriod,
            TimeSpan.FromSeconds(RefillIntervalSeconds)
        ));

        // Try to consume a token
        if (bucket.TryConsume(1))
        {
            // Allow the request to proceed
            await _next(context);
        }
        else
        {
            // Rate limit exceeded
            _logger.LogWarning("Global rate limit exceeded for client {ClientId}", clientId);
            
            var response = new ApiResponse(429, "Too many requests. Please try again later.");
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            
            // Add retry-after header
            context.Response.Headers["Retry-After"] = RefillIntervalSeconds.ToString();
            
            await context.Response.WriteAsJsonAsync(response);
        }
    }

    // Token bucket implementation for rate limiting
    private class TokenBucket
    {
        private readonly int _maxTokens;
        private readonly int _refillAmount;
        private readonly TimeSpan _refillPeriod;
        private double _tokens;
        private DateTime _lastRefill;
        private readonly object _lock = new();

        public TokenBucket(int maxTokens, int refillAmount, TimeSpan refillPeriod)
        {
            _maxTokens = maxTokens;
            _refillAmount = refillAmount;
            _refillPeriod = refillPeriod;
            _tokens = maxTokens;
            _lastRefill = DateTime.UtcNow;
        }

        public bool TryConsume(int count)
        {
            lock (_lock)
            {
                RefillTokens();

                if (_tokens >= count)
                {
                    _tokens -= count;
                    return true;
                }

                return false;
            }
        }

        private void RefillTokens()
        {
            var now = DateTime.UtcNow;
            var timePassed = now - _lastRefill;

            if (timePassed >= _refillPeriod)
            {
                // Calculate how many refill periods have passed
                var periods = Math.Floor(timePassed.TotalSeconds / _refillPeriod.TotalSeconds);
                
                // Add tokens based on periods passed
                var tokensToAdd = periods * _refillAmount;
                
                // Update tokens and last refill time
                _tokens = Math.Min(_maxTokens, _tokens + tokensToAdd);
                _lastRefill = now;
            }
        }
    }
}
