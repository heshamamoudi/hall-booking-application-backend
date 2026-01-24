using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HallApp.Core.Exceptions;

namespace HallApp.Web.Filters;

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
        _logger.LogError(context.Exception, "An unhandled exception occurred: {Message}", context.Exception.Message);

        var response = new ApiResponse
        {
            IsSuccess = false,
            Message = _environment.IsDevelopment() 
                ? context.Exception.Message 
                : "An unexpected error occurred. Please try again later.",
            StatusCode = 500,
            Timestamp = DateTime.UtcNow
        };

        context.Result = new ObjectResult(response) 
        { 
            StatusCode = 500 
        };
        
        context.ExceptionHandled = true;
    }
}
