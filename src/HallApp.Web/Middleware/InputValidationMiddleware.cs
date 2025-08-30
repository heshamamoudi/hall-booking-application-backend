using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HallApp.Core.Exceptions;
using System.Text.RegularExpressions;

namespace HallApp.Web.Middleware;

/// <summary>
/// Middleware to validate input data and prevent injection attacks (SQL, NoSQL, Command, XSS)
/// </summary>
public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;

    // Regex patterns to detect potential injection attacks
    private static readonly Regex SqlInjectionPattern = new(
        @"((;|'|\b)((select|insert|update|delete|drop|alter|create|rename|truncate|backup|exec)\b))|(-{2})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex XssPattern = new(
        @"<script[^>]*>.*?</script>|<.*?javascript:.*?>|<.*?&#.*?>|<.*?on\w+='?.*?'?>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Methods that can have a body to check
    private static readonly string[] MethodsWithBody = { "POST", "PUT", "PATCH" };

    public InputValidationMiddleware(RequestDelegate next, ILogger<InputValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only check methods that can have a body
        if (Array.IndexOf(MethodsWithBody, context.Request.Method) >= 0)
        {
            // Save the request body so it can be read again later
            context.Request.EnableBuffering();

            // Skip validation for file uploads (handled by specific middleware)
            if (!context.Request.HasFormContentType)
            {
                using var reader = new StreamReader(
                    context.Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);
                
                var body = await reader.ReadToEndAsync();
                
                // Reset position to start so the next middleware can read it
                context.Request.Body.Position = 0;

                // Check for potential injection patterns
                if (ContainsInjectionPattern(body))
                {
                    _logger.LogWarning("Potential injection attack detected from {IP}", 
                        context.Connection.RemoteIpAddress);
                    
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    
                    var response = new ApiResponse(400, "Invalid input data detected");
                    await context.Response.WriteAsJsonAsync(response);
                    return;
                }
            }
        }

        // Check query string parameters too
        foreach (var key in context.Request.Query.Keys)
        {
            var value = context.Request.Query[key].ToString();
            if (ContainsInjectionPattern(value))
            {
                _logger.LogWarning("Potential injection attack in query string from {IP}", 
                    context.Connection.RemoteIpAddress);
                
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                
                var response = new ApiResponse(400, "Invalid query parameter detected");
                await context.Response.WriteAsJsonAsync(response);
                return;
            }
        }

        await _next(context);
    }

    private bool ContainsInjectionPattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // Check for SQL injection
        if (SqlInjectionPattern.IsMatch(input))
            return true;

        // Check for XSS
        if (XssPattern.IsMatch(input))
            return true;

        return false;
    }
}

// Extension method
public static class InputValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseInputValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<InputValidationMiddleware>();
    }
}
