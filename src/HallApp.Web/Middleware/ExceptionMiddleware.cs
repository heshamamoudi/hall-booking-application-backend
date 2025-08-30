using System.Net;
using System.Text.Json;
using HallApp.Web.Errors;

namespace HallApp.Web.Middleware;

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
            // Pass the request to the next middleware in the pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception with an error message
            _logger.LogError(ex, "An exception occurred: {Message}", ex.Message);

            // Handle the exception and generate a response
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        // Determine the status code based on the type of exception
        int statusCode = ex switch
        {
            ApiException apiEx => apiEx.StatusCode, // Use the status code from ApiException
            _ => (int)HttpStatusCode.InternalServerError // Default to 500 Internal Server Error for other exceptions
        };

        context.Response.StatusCode = statusCode;

        // Create the response based on the environment (development or production)
        var response = _environment.IsDevelopment()
            ? new ApiException(statusCode, ex.Message, ex.StackTrace?.ToString())
            : new ApiException(statusCode, "A server error occurred.");

        // Serialize the response to JSON
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var jsonResponse = JsonSerializer.Serialize(response, options);

        // Write the JSON response to the context
        await context.Response.WriteAsync(jsonResponse);
    }
}
