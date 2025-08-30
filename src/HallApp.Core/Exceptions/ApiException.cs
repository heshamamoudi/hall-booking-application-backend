using Microsoft.AspNetCore.Identity;

namespace HallApp.Core.Exceptions;

/// <summary>
/// Represents an API-specific exception with additional context such as status code, error details, and identity errors.
/// </summary>
public class ApiException : Exception
{
    /// <summary>
    /// Gets the HTTP status code associated with the exception.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the collection of identity errors, if any.
    /// </summary>
    public IEnumerable<IdentityError> Errors { get; }

    /// <summary>
    /// Gets additional details about the exception, typically used in development environments.
    /// </summary>
    public string Details { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException"/> class with a specified status code and error message.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">The error message.</param>
    public ApiException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException"/> class with a specified status code, error message, and identity errors.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="errors">The collection of identity errors.</param>
    public ApiException(int statusCode, string message, IEnumerable<IdentityError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException"/> class with a specified status code, error message, and additional details.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="details">Additional details about the exception.</param>
    public ApiException(int statusCode, string message, string details)
        : base(message)
    {
        StatusCode = statusCode;
        Details = details;
    }
}
