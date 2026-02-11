using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HallApp.Core.Exceptions;
using FluentValidation;

namespace HallApp.Web.Filters;

/// <summary>
/// Global exception filter that catches unhandled exceptions and converts them
/// to consistent ApiResponse error payloads. Handles specific exception types
/// (ApiException, ValidationException, KeyNotFoundException, etc.) with
/// appropriate HTTP status codes.
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        var (statusCode, response) = context.Exception switch
        {
            ApiException apiEx => HandleApiException(apiEx),
            ValidationException validationEx => HandleValidationException(validationEx),
            KeyNotFoundException notFoundEx => HandleNotFoundException(notFoundEx),
            UnauthorizedAccessException unauthorizedEx => HandleUnauthorizedException(unauthorizedEx),
            ArgumentException argEx => HandleArgumentException(argEx),
            OperationCanceledException => HandleOperationCancelledException(),
            _ => HandleUnknownException(context.Exception)
        };

        // Only log at Error level for 500s; use Warning for 4xx client errors
        if (statusCode >= 500)
        {
            _logger.LogError(context.Exception,
                "Unhandled exception on {Method} {Path}: {Message}",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                context.Exception.Message);
        }
        else
        {
            _logger.LogWarning(
                "Client error on {Method} {Path}: {StatusCode} - {Message}",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                statusCode,
                context.Exception.Message);
        }

        context.Result = new ObjectResult(response) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }

    private (int, ApiResponse) HandleApiException(ApiException ex)
    {
        return (ex.StatusCode, new ApiResponse
        {
            IsSuccess = false,
            Message = ex.Message,
            StatusCode = ex.StatusCode,
            Timestamp = DateTime.UtcNow
        });
    }

    private (int, object) HandleValidationException(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return (400, new
        {
            statusCode = 400,
            message = "One or more validation errors occurred.",
            isSuccess = false,
            errors,
            timestamp = DateTime.UtcNow
        });
    }

    private (int, ApiResponse) HandleNotFoundException(KeyNotFoundException ex)
    {
        return (404, new ApiResponse
        {
            IsSuccess = false,
            Message = ex.Message,
            StatusCode = 404,
            Timestamp = DateTime.UtcNow
        });
    }

    private (int, ApiResponse) HandleUnauthorizedException(UnauthorizedAccessException ex)
    {
        return (401, new ApiResponse
        {
            IsSuccess = false,
            Message = "You are not authorized to perform this action.",
            StatusCode = 401,
            Timestamp = DateTime.UtcNow
        });
    }

    private (int, ApiResponse) HandleArgumentException(ArgumentException ex)
    {
        return (400, new ApiResponse
        {
            IsSuccess = false,
            Message = _environment.IsDevelopment() ? ex.Message : "Invalid request data.",
            StatusCode = 400,
            Timestamp = DateTime.UtcNow
        });
    }

    private (int, ApiResponse) HandleOperationCancelledException()
    {
        return (499, new ApiResponse
        {
            IsSuccess = false,
            Message = "Request was cancelled.",
            StatusCode = 499,
            Timestamp = DateTime.UtcNow
        });
    }

    private (int, ApiResponse) HandleUnknownException(Exception ex)
    {
        return (500, new ApiResponse
        {
            IsSuccess = false,
            Message = _environment.IsDevelopment()
                ? ex.Message
                : "An unexpected error occurred. Please try again later.",
            StatusCode = 500,
            Timestamp = DateTime.UtcNow
        });
    }
}
