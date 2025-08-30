namespace HallApp.Core.Exceptions;

public class ApiResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public object? Data { get; set; }

    public ApiResponse(int statusCode, string message = null, object? data = null)
    {
        StatusCode = statusCode;
        Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        Data = data;
    }

    private string GetDefaultMessageForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad request",
            401 => "You are not authorized",
            403 => "Forbidden",
            404 => "Resource not found",
            500 => "Internal server error",
            _ => null
        };
    }
}
