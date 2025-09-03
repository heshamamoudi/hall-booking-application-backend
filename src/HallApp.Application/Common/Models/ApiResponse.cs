namespace HallApp.Application.Common.Models
{
    /// <summary>
    /// Standardized API response wrapper without data
    /// </summary>
    public class ApiResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Standardized API response wrapper with generic data
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    public class ApiResponse<T> : ApiResponse
    {
        public T Data { get; set; }
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
    }
}
