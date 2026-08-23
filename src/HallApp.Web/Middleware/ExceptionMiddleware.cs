using System.Net;
using System.Text.Json;
using HallApp.Core.Exceptions;

namespace HallApp.Web.Middleware;

/// <summary>
/// Global exception handler middleware (CRIT-SEC-003).
/// Catches all unhandled exceptions and converts them to safe JSON responses.
/// Includes correlation IDs for tracing, hides internal details in production.
/// This is the outermost catch-all -- the ApiExceptionFilter handles most
/// controller-level exceptions before they reach here.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Generate a correlation ID for tracing this error across logs and support requests
        var correlationId = Guid.NewGuid().ToString("N")[..12];

        // Log full exception details server-side with correlation ID, path, and method
        _logger.LogError(ex,
            "CRIT-SEC-003: Unhandled exception. CorrelationId: {CorrelationId}, " +
            "Path: {Path}, Method: {Method}, RemoteIp: {RemoteIp}",
            correlationId,
            context.Request.Path,
            context.Request.Method,
            context.Connection.RemoteIpAddress);

        // Once the response has started the headers are read-only, and setting
        // ContentType below throws InvalidOperationException - which then replaces
        // the real exception in the logs with a misleading one and leaves the client
        // holding a half-written body. This happens when serialization itself throws
        // partway through writing. Fail the connection instead: a client that sees a
        // dropped connection retries, one that sees truncated JSON does not.
        if (context.Response.HasStarted)
        {
            _logger.LogError(ex,
                "Exception after the response started; cannot write an error body. " +
                "CorrelationId: {CorrelationId}",
                correlationId);
            context.Abort();
            return;
        }

        context.Response.ContentType = "application/json";

        // Determine status code based on exception type
        int statusCode = ex switch
        {
            ApiException apiEx => apiEx.StatusCode,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            OperationCanceledException => 499, // Client closed request
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = statusCode;

        // Determine the user-facing message:
        // - In Development: show exception details for debugging
        // - In Production: return generic message to avoid leaking internals
        string message;
        string? details = null;

        if (_environment.IsDevelopment())
        {
            message = ex.Message;
            details = ex.StackTrace;
        }
        else
        {
            message = statusCode switch
            {
                401 => "You are not authorized to perform this action.",
                404 => "The requested resource was not found.",
                400 => "Invalid request data.",
                499 => "Request was cancelled.",
                _ => "An error occurred processing your request. Please contact support if the issue persists."
            };
        }

        var response = new
        {
            statusCode,
            message,
            details = _environment.IsDevelopment() ? details : null,
            correlationId,
            isSuccess = false,
            timestamp = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var jsonResponse = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(jsonResponse);
    }
}
