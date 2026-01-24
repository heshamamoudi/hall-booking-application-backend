namespace HallApp.Core.Exceptions;

/// <summary>
/// Standardized API response wrapper - single source of truth for all API responses
/// </summary>
public class ApiResponse
{
    private bool? _isSuccess;

    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the operation was successful. Defaults to true for 2xx status codes.
    /// Can be explicitly set to override the default behavior.
    /// </summary>
    public bool IsSuccess
    {
        get => _isSuccess ?? (StatusCode >= 200 && StatusCode < 300);
        set => _isSuccess = value;
    }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiResponse()
    {
    }

    public ApiResponse(int statusCode, string? message = null)
    {
        StatusCode = statusCode;
        Message = message ?? GetDefaultMessageForStatusCode(statusCode);
    }

    /// <summary>
    /// Constructor with data for backward compatibility
    /// </summary>
    public ApiResponse(int statusCode, string message, object? data)
    {
        StatusCode = statusCode;
        Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        Data = data;
    }

    /// <summary>
    /// Optional data payload for non-generic responses
    /// </summary>
    public object? Data { get; set; }

    public static string GetDefaultMessageForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            200 => "Success",
            201 => "Created",
            204 => "No content",
            400 => "Bad request",
            401 => "You are not authorized",
            403 => "Forbidden",
            404 => "Resource not found",
            409 => "Conflict",
            422 => "Validation error",
            429 => "Too many requests",
            500 => "Internal server error",
            503 => "Service unavailable",
            _ => "Unknown error"
        };
    }
}

/// <summary>
/// Standardized API response wrapper with generic data
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }

    public ApiResponse()
    {
    }

    public ApiResponse(int statusCode, string? message = null, T? data = default) : base(statusCode, message)
    {
        Data = data;
    }

    public static ApiResponse<T> Success(T data, string message = "Success")
    {
        return new ApiResponse<T>(200, message, data);
    }

    public static ApiResponse<T> Created(T data, string message = "Created successfully")
    {
        return new ApiResponse<T>(201, message, data);
    }

    public static ApiResponse<T> NotFound(string message = "Resource not found")
    {
        return new ApiResponse<T>(404, message);
    }

    public static ApiResponse<T> BadRequest(string message = "Bad request")
    {
        return new ApiResponse<T>(400, message);
    }

    public static ApiResponse<T> Error(string message = "An error occurred", int statusCode = 500)
    {
        return new ApiResponse<T>(statusCode, message);
    }
}

/// <summary>
/// Paginated API response wrapper
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class PaginatedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    public PaginatedApiResponse()
    {
    }

    public PaginatedApiResponse(IEnumerable<T> data, int currentPage, int pageSize, int totalCount)
        : base(200, "Success", data)
    {
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
