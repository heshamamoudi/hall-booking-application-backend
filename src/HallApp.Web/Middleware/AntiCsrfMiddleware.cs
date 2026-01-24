using Microsoft.AspNetCore.Antiforgery;
using HallApp.Core.Exceptions;

namespace HallApp.Web.Middleware;

/// <summary>
/// Middleware to prevent Cross-Site Request Forgery (CSRF) attacks using ASP.NET Core's built-in antiforgery services
/// </summary>
public class AntiCsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<AntiCsrfMiddleware> _logger;
    private readonly string[] _safeMethods = { "GET", "HEAD", "OPTIONS", "TRACE" };
    private readonly string[] _publicEndpoints = { "/api/customer/login", "/api/customer/register" };

    public AntiCsrfMiddleware(RequestDelegate next, IAntiforgery antiforgery, ILogger<AntiCsrfMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _antiforgery = antiforgery ?? throw new ArgumentNullException(nameof(antiforgery));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Skip CSRF validation for safe HTTP methods
            if (Array.Exists(_safeMethods, method => string.Equals(context.Request.Method, method, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Skip CSRF validation for public endpoints (like login/register)
            if (Array.Exists(_publicEndpoints, endpoint => context.Request.Path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Validate the antiforgery token for unsafe methods
            await _antiforgery.ValidateRequestAsync(context);
            _logger.LogDebug("CSRF token validation passed for {Method} {Path}", context.Request.Method, context.Request.Path);

            await _next(context);
        }
        catch (AntiforgeryValidationException ex)
        {
            _logger.LogWarning(ex, "CSRF token validation failed for {Method} {Path} from {RemoteIpAddress}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            // Return a 400 Bad Request with a specific error message
            throw new ApiException(400, "Invalid or missing CSRF token. Please refresh the page and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in CSRF middleware for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
            throw;
        }
    }
}
