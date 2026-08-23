using Microsoft.AspNetCore.Mvc;
using HallApp.Core.Exceptions;
using System.Security.Claims;

namespace HallApp.Web.Controllers.Common
{
    /// <summary>
    /// Enhanced base controller providing common functionality for all API controllers
    /// Implements consistent error handling, user context, and response patterns
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
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
        /// Reconciles the two paging conventions in use.
        ///
        /// The API was written around 1-based pageNumber/pageSize, but the Angular
        /// clients send 0-based page/size. Model binding quietly ignored the ones it
        /// did not recognise, so every paged list returned page 1 no matter what the
        /// caller asked for. Both spellings are honoured here: page/size wins when
        /// present, and is converted from 0-based to 1-based.
        /// </summary>
        protected static (int PageNumber, int PageSize) ResolvePaging(
            int pageNumber, int pageSize, int? page, int? size)
        {
            if (page.HasValue) pageNumber = Math.Max(0, page.Value) + 1;
            if (size.HasValue) pageSize = size.Value;
            return (pageNumber, pageSize);
        }

        /// <summary>
        /// Normalises an inbound date to UTC.
        ///
        /// Dates bound from a query string or JSON body arrive with
        /// Kind=Unspecified, and Npgsql refuses to write anything but UTC into a
        /// timestamptz column - it throws rather than guessing. A naive value is
        /// treated as already being UTC rather than shifted by the server's
        /// timezone, which would silently move the caller's date.
        /// </summary>
        protected static DateTime AsUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

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
        /// Checks if the current user is a hall organization manager (hall organization owner)
        /// </summary>
        protected bool IsHallOrganizationManager => HasRole("HallOrganizationManager");

        /// <summary>
        /// Checks if the current user is a vendor organization manager (vendor organization owner)
        /// </summary>
        protected bool IsVendorOrganizationManager => HasRole("VendorOrganizationManager");

        /// <summary>
        /// Checks if the current user is a hall manager (team member assigned to specific halls)
        /// </summary>
        protected bool IsHallManager => HasRole("HallManager");

        /// <summary>
        /// Checks if the current user is a vendor manager (team member assigned to specific vendors)
        /// </summary>
        protected bool IsVendorManager => HasRole("VendorManager");

        /// <summary>
        /// Checks if the current user can manage halls (HallOrganizationManager or HallManager)
        /// </summary>
        protected bool CanManageHalls => IsHallOrganizationManager || IsHallManager;

        /// <summary>
        /// Checks if the current user can manage vendors (VendorOrganizationManager or VendorManager)
        /// </summary>
        protected bool CanManageVendors => IsVendorOrganizationManager || IsVendorManager;

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
