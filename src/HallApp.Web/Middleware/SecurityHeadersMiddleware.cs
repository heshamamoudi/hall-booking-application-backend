using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace HallApp.Web.Middleware;

/// <summary>
/// Middleware to add security headers according to OWASP guidelines
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _isDevelopment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _isDevelopment = env.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Don't apply restrictive headers to Swagger paths
        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            // Content-Type Options
            // Prevents browsers from interpreting files as a different MIME type
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            
            // X-Frame-Options
            // Protects against clickjacking attacks
            context.Response.Headers["X-Frame-Options"] = "DENY";
            
            // X-XSS-Protection
            // Enables the Cross-site scripting (XSS) filter in browsers
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            
            // Referrer-Policy
            // Controls how much referrer information should be included with requests
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            
            // Strict-Transport-Security (HSTS)
            // Enforces HTTPS connection for a specified period
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            
            // Permissions-Policy (formerly Feature-Policy)
            // Controls which browser features and APIs can be used
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            
            // Content-Security-Policy
            // More comprehensive CSP that follows OWASP recommendations
            var csp = "default-src 'self'; " +
                      "script-src 'self'; " +
                      "style-src 'self'; " +
                      "img-src 'self' data:; " +
                      "font-src 'self'; " +
                      "connect-src 'self'; " +
                      "media-src 'self'; " +
                      "object-src 'none'; " +
                      "frame-src 'none'; " +
                      "base-uri 'self'; " +
                      "form-action 'self'";
            
            // In development, we might need to relax some CSP rules
            if (_isDevelopment)
            {
                csp = "default-src 'self'; " +
                      "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                      "style-src 'self' 'unsafe-inline'; " +
                      "img-src 'self' data:; " +
                      "font-src 'self'; " +
                      "connect-src 'self'; " +
                      "media-src 'self'";
            }
            
            context.Response.Headers["Content-Security-Policy"] = csp;

            // Clear-Site-Data
            // Used for logout to clear browser data
            if (context.Request.Path.StartsWithSegments("/api/customer/logout"))
            {
                context.Response.Headers["Clear-Site-Data"] = "\"cache\", \"cookies\", \"storage\"";
            }
        }

        await _next(context);
    }
}

// Extension method to easily add this middleware to the pipeline
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
