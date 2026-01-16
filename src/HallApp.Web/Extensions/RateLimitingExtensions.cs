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
        /// </summary>
        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                
                // Global rate limit policy - applied to all endpoints
                options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                    // Burst protection: Only 10 requests per second from same IP (prevents 100 reqs in 1 sec)
                    PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                        // Skip rate limiting for localhost
                        if (clientIp == "::1" || clientIp == "127.0.0.1" || clientIp == "::ffff:127.0.0.1")
                        {
                            return RateLimitPartition.GetNoLimiter("localhost");
                        }

                        // Skip rate limiting for authenticated users
                        if (context.User?.Identity?.IsAuthenticated == true)
                        {
                            return RateLimitPartition.GetNoLimiter($"authenticated-{context.User.Identity.Name}");
                        }

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: clientIp,
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                Window = TimeSpan.FromSeconds(1),
                                PermitLimit = 10, // Max 10 requests per second
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            });
                    }),
                    
                    // Layer 2: Short-term limit - protection against sustained medium-rate attacks
                    PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                        // Skip rate limiting for localhost
                        if (clientIp == "::1" || clientIp == "127.0.0.1" || clientIp == "::ffff:127.0.0.1")
                        {
                            return RateLimitPartition.GetNoLimiter("localhost");
                        }

                        // Skip rate limiting for authenticated users
                        if (context.User?.Identity?.IsAuthenticated == true)
                        {
                            return RateLimitPartition.GetNoLimiter($"authenticated-{context.User.Identity.Name}");
                        }

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: clientIp,
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                Window = TimeSpan.FromSeconds(10),
                                PermitLimit = 30, // Max 30 requests per 10 seconds
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            });
                    }),
                    
                    // Layer 3: Overall limit - general rate limiting
                    PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                        // Skip rate limiting for localhost
                        if (clientIp == "::1" || clientIp == "127.0.0.1" || clientIp == "::ffff:127.0.0.1")
                        {
                            return RateLimitPartition.GetNoLimiter("localhost");
                        }

                        // Skip rate limiting for authenticated users
                        if (context.User?.Identity?.IsAuthenticated == true)
                        {
                            return RateLimitPartition.GetNoLimiter($"authenticated-{context.User.Identity.Name}");
                        }

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: clientIp,
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
    }
}
