using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.RateLimiting;

namespace HallApp.Web.Extensions
{
    public static class RateLimitingExtensions
    {
        /// <summary>
        /// Adds rate limiting services using the built-in ASP.NET Core Rate Limiting feature
        /// to prevent brute force and DoS attacks (OWASP A07:2021)
        ///
        /// Best Practice Implementation:
        /// - Authenticated users: NO rate limiting (unlimited access)
        /// - Unauthenticated users: Strict rate limiting to prevent abuse
        /// - Login endpoints: Extra strict limits to prevent brute force attacks
        /// - Localhost: No rate limiting for development
        /// </summary>
        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Global rate limit policy - ONLY applies to unauthenticated requests
                // Authenticated users bypass all global rate limits
                options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                    // Layer 1: Burst protection - prevents rapid-fire requests from bots/scripts
                    // 10 requests per second per IP for unauthenticated users
                    PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        // Skip rate limiting for localhost (development)
                        if (IsLocalhost(context))
                        {
                            return RateLimitPartition.GetNoLimiter("localhost");
                        }

                        // BEST PRACTICE: Authenticated users have unlimited access
                        if (context.User?.Identity?.IsAuthenticated == true)
                        {
                            return RateLimitPartition.GetNoLimiter($"authenticated-{context.User.Identity.Name}");
                        }

                        // Rate limit unauthenticated requests by IP
                        var clientIp = GetClientIp(context);
                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: $"burst:{clientIp}",
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                Window = TimeSpan.FromSeconds(1),
                                PermitLimit = 10, // Max 10 requests per second
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            });
                    }),

                    // Layer 2: Short-term protection - prevents sustained attacks
                    // 30 requests per 10 seconds for unauthenticated users
                    PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        if (IsLocalhost(context))
                        {
                            return RateLimitPartition.GetNoLimiter("localhost");
                        }

                        if (context.User?.Identity?.IsAuthenticated == true)
                        {
                            return RateLimitPartition.GetNoLimiter($"authenticated-{context.User.Identity.Name}");
                        }

                        var clientIp = GetClientIp(context);
                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: $"short:{clientIp}",
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                Window = TimeSpan.FromSeconds(10),
                                PermitLimit = 30, // Max 30 requests per 10 seconds
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            });
                    }),

                    // Layer 3: Overall limit - general protection
                    // 100 requests per minute for unauthenticated users
                    PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        if (IsLocalhost(context))
                        {
                            return RateLimitPartition.GetNoLimiter("localhost");
                        }

                        if (context.User?.Identity?.IsAuthenticated == true)
                        {
                            return RateLimitPartition.GetNoLimiter($"authenticated-{context.User.Identity.Name}");
                        }

                        var clientIp = GetClientIp(context);
                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: $"overall:{clientIp}",
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                Window = TimeSpan.FromMinutes(1),
                                PermitLimit = 100, // Max 100 requests per minute
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            });
                    })
                );
                
                // OWASP A07:2021 Identification and Authentication Failures protection
                // More restrictive policy for login attempts to prevent brute force
                options.AddFixedWindowLimiter("login", options =>
                {
                    options.Window = TimeSpan.FromMinutes(5);
                    options.PermitLimit = 5; // Strict limit: only 5 attempts per 5 minutes
                    options.QueueLimit = 0;
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
                
                // Additional protection for repeated login failures across longer time periods
                options.AddFixedWindowLimiter("login-lockout", options =>
                {
                    options.Window = TimeSpan.FromHours(1);
                    options.PermitLimit = 10; // Maximum 10 attempts per hour before extended lockout
                    options.QueueLimit = 0;
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
                
                // Policy for general API endpoints
                options.AddFixedWindowLimiter("api", options =>
                {
                    options.Window = TimeSpan.FromMinutes(1);
                    options.PermitLimit = 60;
                    options.QueueLimit = 0;
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
                
                // OnRejected handler - customize the response when rate limit is hit
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.Headers["Retry-After"] = "60";
                    
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        await context.HttpContext.Response.WriteAsJsonAsync(new
                        {
                            statusCode = 429,
                            message = "Too many requests. Please try again later.",
                            retryAfter = retryAfter.TotalSeconds
                        }, cancellationToken);
                    }
                    else
                    {
                        await context.HttpContext.Response.WriteAsJsonAsync(new
                        {
                            statusCode = 429,
                            message = "Too many requests. Please try again later."
                        }, cancellationToken);
                    }
                };
            });

            return services;
        }

        /// <summary>
        /// Check if the request is from localhost (development)
        /// </summary>
        private static bool IsLocalhost(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "";
            return clientIp == "::1" || clientIp == "127.0.0.1" || clientIp == "::ffff:127.0.0.1";
        }

        /// <summary>
        /// Get the client IP address, considering proxy headers
        /// </summary>
        private static string GetClientIp(HttpContext context)
        {
            // Check for forwarded IP (behind load balancer/proxy)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Take the first IP in the chain (original client)
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check for real IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp.Trim();
            }

            // Fall back to connection remote IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        }
    }
}
