using Microsoft.AspNetCore.Mvc;
using HallApp.Application.Common.Models;
using System.Security.Claims;

namespace HallApp.Web.Controllers.Common
{
    /// <summary>
    /// Enhanced base controller providing common functionality for all API controllers
    /// Implements consistent error handling, user context, and response patterns
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Gets the current user's ID from the JWT token claims
        /// </summary>
        protected int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        /// <summary>
        /// Gets the current user's username from the JWT token claims
        /// </summary>
        protected string UserName => User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

        /// <summary>
        /// Gets the current user's email from the JWT token claims
        /// </summary>
        protected string UserEmail => User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

        /// <summary>
        /// Gets the current user's roles from the JWT token claims
        /// </summary>
        protected IEnumerable<string> UserRoles => User.FindAll(ClaimTypes.Role).Select(c => c.Value);

        /// <summary>
        /// Checks if the current user has a specific role
        /// </summary>
        /// <param name="role">Role to check</param>
        /// <returns>True if user has the role, false otherwise</returns>
        protected bool HasRole(string role) => UserRoles.Contains(role);

        /// <summary>
        /// Checks if the current user is an admin
        /// </summary>
        protected bool IsAdmin => HasRole("Admin");

        /// <summary>
        /// Checks if the current user is a hall manager
        /// </summary>
        protected bool IsHallManager => HasRole("HallManager");

        /// <summary>
        /// Checks if the current user is a customer
        /// </summary>
        protected bool IsCustomer => HasRole("Customer");

        /// <summary>
        /// Returns a standardized success response
        /// </summary>
        /// <param name="data">Data to return</param>
        /// <param name="message">Success message</param>
        /// <returns>ApiResponse with success status</returns>
        protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Operation completed successfully")
        {
            return Ok(new ApiResponse<T>
            {
                StatusCode = 200,
                Message = message,
                Data = data,
                IsSuccess = true
            });
        }

        /// <summary>
        /// Returns a standardized success response without data
        /// </summary>
        /// <param name="message">Success message</param>
        /// <returns>ApiResponse with success status</returns>
        protected ActionResult<ApiResponse> Success(string message = "Operation completed successfully")
        {
            return Ok(new ApiResponse
            {
                StatusCode = 200,
                Message = message,
                IsSuccess = true
            });
        }

        /// <summary>
        /// Returns a standardized error response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>ApiResponse with error status</returns>
        protected ActionResult<ApiResponse> Error(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new ApiResponse
            {
                StatusCode = statusCode,
                Message = message,
                IsSuccess = false
            });
        }

        /// <summary>
        /// Returns a standardized error response with typed data
        /// </summary>
        /// <typeparam name="T">Type of data</typeparam>
        /// <param name="message">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>ApiResponse with error status</returns>
        protected ActionResult<ApiResponse<T>> Error<T>(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new ApiResponse<T>
            {
                StatusCode = statusCode,
                Message = message,
                IsSuccess = false,
                Data = default(T)
            });
        }

        /// <summary>
        /// Returns a not found response
        /// </summary>
        /// <param name="message">Not found message</param>
        /// <returns>404 ApiResponse</returns>
        protected ActionResult<ApiResponse> NotFound(string message = "Resource not found")
        {
            return NotFound(new ApiResponse
            {
                StatusCode = 404,
                Message = message,
                IsSuccess = false
            });
        }

        /// <summary>
        /// Returns an unauthorized response
        /// </summary>
        /// <param name="message">Unauthorized message</param>
        /// <returns>401 ApiResponse</returns>
        protected ActionResult<ApiResponse> Unauthorized(string message = "You are not authorized to perform this action")
        {
            return Unauthorized(new ApiResponse
            {
                StatusCode = 401,
                Message = message,
                IsSuccess = false
            });
        }

        /// <summary>
        /// Returns a forbidden response
        /// </summary>
        /// <param name="message">Forbidden message</param>
        /// <returns>403 ApiResponse</returns>
        protected ActionResult<ApiResponse> Forbidden(string message = "You do not have permission to perform this action")
        {
            return StatusCode(403, new ApiResponse
            {
                StatusCode = 403,
                Message = message,
                IsSuccess = false
            });
        }

        /// <summary>
        /// Validates that the current user can access a resource belonging to a specific user
        /// </summary>
        /// <param name="resourceUserId">The user ID that owns the resource</param>
        /// <returns>True if access is allowed, false otherwise</returns>
        protected bool CanAccessUserResource(int resourceUserId)
        {
            return IsAdmin || UserId == resourceUserId;
        }
    }
}
