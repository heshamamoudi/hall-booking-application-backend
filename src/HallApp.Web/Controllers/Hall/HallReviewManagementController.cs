using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HallApp.Application.DTOs.Review;
using HallApp.Application.Services;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using System.Security.Claims;

namespace HallApp.Web.Controllers.Hall;

/// <summary>
/// Hall review management endpoints for hall managers.
/// Allows managers to view reviews, respond to them, view statistics, and flag inappropriate reviews.
/// Requires HallManager or Admin role. HallManagers can only access reviews for their own halls.
/// </summary>
[Authorize(Roles = "Admin,HallOrganizationManager,HallManager")]
[Route("api/halls")]
[ApiController]
public class HallReviewManagementController : BaseApiController
{
    private readonly IHallReviewManagementService _reviewManagementService;
    private readonly IHallQueryService _hallQueryService;
    private readonly IOrganizationService _organizationService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<HallReviewManagementController> _logger;

    public HallReviewManagementController(
        IHallReviewManagementService reviewManagementService,
        IHallQueryService hallQueryService,
        IOrganizationService organizationService,
        INotificationService notificationService,
        ILogger<HallReviewManagementController> logger)
    {
        _reviewManagementService = reviewManagementService;
        _hallQueryService = hallQueryService;
        _organizationService = organizationService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Verifies the current user has access to the specified hall.
    /// Admins have access to all halls. HallManagers only have access to halls they manage.
    /// </summary>
    // AUTH-FIX: Also checks organization ownership for HallOrganizationManager users,
    // who access halls through Organization -> Hall (not through HallManager join table).
    private async Task<bool> UserOwnsHall(int hallId)
    {
        if (User.IsInRole("Admin")) return true;

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return false;

        // Check 1: HallManager team members -- halls linked via HallManager join table
        var managerHalls = await _hallQueryService.GetHallsByManagerAsync(userId);
        if (managerHalls.Any(h => h.ID == hallId))
            return true;

        // Check 2: HallOrganizationManager owners -- halls linked via Organization
        // Organization owners do NOT have HallManager entities; they access halls
        // through their Organization -> Hall relationship.
        if (User.IsInRole("HallOrganizationManager"))
        {
            var userIdInt = int.Parse(userId);
            var organization = await _organizationService.GetOrganizationByOwnerId(userIdInt);
            if (organization != null)
            {
                var orgHalls = await _hallQueryService.GetOrganizationHallsAsync(organization.Id);
                if (orgHalls.Any(h => h.ID == hallId))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get reviews for a specific hall with optional filtering and pagination
    /// </summary>
    /// <remarks>
    /// Returns paginated reviews for the hall with customer details and manager response info.
    ///
    /// Filter options:
    /// - rating: Filter by specific star rating (1-5)
    /// - hasResponse: Filter by whether the manager has responded (true/false)
    /// - isFlagged: Filter by flagged status (true/false)
    /// - fromDate: Filter reviews created on or after this date (ISO 8601)
    /// - toDate: Filter reviews created on or before this date (ISO 8601)
    /// - sortBy: Sort field ("created", "rating", "responded"). Default: "created"
    /// - sortDescending: Sort direction. Default: true (newest first)
    ///
    /// Authorization: Admin can view any hall. HallManagers can only view halls they manage.
    /// </remarks>
    /// <param name="id">The hall ID</param>
    /// <param name="rating">Filter by star rating (1-5)</param>
    /// <param name="hasResponse">Filter by response status</param>
    /// <param name="isFlagged">Filter by flagged status</param>
    /// <param name="fromDate">Filter reviews from this date</param>
    /// <param name="toDate">Filter reviews up to this date</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 50)</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDescending">Sort direction</param>
    /// <response code="200">Returns paginated reviews</response>
    /// <response code="403">User does not have access to this hall</response>
    /// <response code="404">Hall not found</response>
    [HttpGet("{id:int}/reviews")]
    [ProducesResponseType(typeof(PaginatedApiResponse<ManagerReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedApiResponse<ManagerReviewDto>>> GetHallReviews(
        int id,
        [FromQuery] int? rating = null,
        [FromQuery] bool? hasResponse = null,
        [FromQuery] bool? isFlagged = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "created",
        [FromQuery] bool sortDescending = true)
    {
        try
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            // Validate rating if provided
            if (rating.HasValue && (rating.Value < 1 || rating.Value > 5))
            {
                return StatusCode(400, new PaginatedApiResponse<ManagerReviewDto>
                {
                    StatusCode = 400,
                    Message = "Rating must be between 1 and 5",
                    IsSuccess = false
                });
            }

            // Verify hall exists
            var hall = await _hallQueryService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return StatusCode(404, new PaginatedApiResponse<ManagerReviewDto>
                {
                    StatusCode = 404,
                    Message = "Hall not found",
                    IsSuccess = false
                });
            }

            // Verify ownership
            if (!await UserOwnsHall(id))
            {
                return StatusCode(403, new PaginatedApiResponse<ManagerReviewDto>
                {
                    StatusCode = 403,
                    Message = "You do not have permission to view reviews for this hall",
                    IsSuccess = false
                });
            }

            var (reviews, totalCount) = await _reviewManagementService.GetHallReviewsAsync(
                id, rating, hasResponse, isFlagged, fromDate, toDate,
                page, pageSize, sortBy, sortDescending);

            var response = new PaginatedApiResponse<ManagerReviewDto>(
                reviews, page, pageSize, totalCount);
            response.Message = $"Retrieved {reviews.Count()} reviews for hall";

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reviews for Hall {HallId}", id);
            return StatusCode(500, new PaginatedApiResponse<ManagerReviewDto>
            {
                StatusCode = 500,
                Message = "An error occurred while loading reviews. Please try again.",
                IsSuccess = false
            });
        }
    }

    /// <summary>
    /// Get review statistics and metrics for a specific hall
    /// </summary>
    /// <remarks>
    /// Returns aggregate review metrics:
    /// - Total reviews count
    /// - Average rating
    /// - Responded vs not responded counts
    /// - Flagged reviews count
    /// - Rating distribution (1-5 star breakdown)
    ///
    /// Authorization: Admin can view any hall. HallManagers can only view halls they manage.
    /// </remarks>
    /// <param name="id">The hall ID</param>
    /// <response code="200">Returns review statistics</response>
    /// <response code="403">User does not have access to this hall</response>
    /// <response code="404">Hall not found</response>
    [HttpGet("{id:int}/reviews/stats")]
    [ProducesResponseType(typeof(ApiResponse<ManagerReviewStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ManagerReviewStatsDto>>> GetHallReviewStats(int id)
    {
        try
        {
            // Verify hall exists
            var hall = await _hallQueryService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return Error<ManagerReviewStatsDto>("Hall not found", 404);
            }

            // Verify ownership
            if (!await UserOwnsHall(id))
            {
                return Error<ManagerReviewStatsDto>(
                    "You do not have permission to view review statistics for this hall", 403);
            }

            var stats = await _reviewManagementService.GetHallReviewStatsAsync(id);
            return Success(stats, "Review statistics retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving review statistics for Hall {HallId}", id);
            return Error<ManagerReviewStatsDto>(
                "An error occurred while loading review statistics. Please try again.", 500);
        }
    }

    /// <summary>
    /// Respond to a customer review
    /// </summary>
    /// <remarks>
    /// Adds a manager response to a customer review.
    ///
    /// Business rules:
    /// - Only one response per review (cannot update/delete once submitted)
    /// - Manager must own the hall that the review belongs to
    /// - Response is visible to all users
    ///
    /// Authorization: Admin can respond to any review. HallManagers can only respond to reviews for halls they manage.
    /// </remarks>
    /// <param name="id">The hall ID (for authorization)</param>
    /// <param name="reviewId">The review ID to respond to</param>
    /// <param name="request">The response text</param>
    /// <response code="200">Response added successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="403">User does not have access to this hall</response>
    /// <response code="404">Review not found</response>
    /// <response code="409">Review already has a response</response>
    [HttpPost("{id:int}/reviews/{reviewId:int}/respond")]
    [ProducesResponseType(typeof(ApiResponse<ManagerReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ManagerReviewDto>>> RespondToReview(
        int id, int reviewId, [FromBody] ReviewResponseRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Error<ManagerReviewDto>($"Invalid response data: {errors}", 400);
            }

            // Verify hall exists
            var hall = await _hallQueryService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return Error<ManagerReviewDto>("Hall not found", 404);
            }

            // Verify ownership
            if (!await UserOwnsHall(id))
            {
                return Error<ManagerReviewDto>(
                    "You do not have permission to respond to reviews for this hall", 403);
            }

            // Verify the review belongs to this hall
            var existingReview = await _reviewManagementService.GetReviewByIdAsync(reviewId);
            if (existingReview == null)
            {
                return Error<ManagerReviewDto>("Review not found", 404);
            }

            if (existingReview.HallId != id)
            {
                return Error<ManagerReviewDto>("Review does not belong to this hall", 400);
            }

            var result = await _reviewManagementService.RespondToReviewAsync(
                reviewId, request.Response, UserId);

            // Send notification to customer about the response (fire-and-forget)
            if (existingReview.CustomerId.HasValue)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            existingReview.CustomerId.Value,
                            "Manager Responded to Your Review",
                            $"The hall manager has responded to your review for {existingReview.HallName}.",
                            "Review"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to send notification for review response. ReviewId: {ReviewId}",
                            reviewId);
                    }
                });
            }

            return Success(result, "Response added successfully");
        }
        catch (KeyNotFoundException)
        {
            return Error<ManagerReviewDto>("Review not found", 404);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot respond to Review {ReviewId}: {Message}", reviewId, ex.Message);
            return Error<ManagerReviewDto>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to Review {ReviewId} for Hall {HallId}", reviewId, id);
            return Error<ManagerReviewDto>(
                "An error occurred while submitting the response. Please try again.", 500);
        }
    }

    /// <summary>
    /// Flag a review as inappropriate
    /// </summary>
    /// <remarks>
    /// Marks a review as inappropriate for admin review.
    /// The review remains visible but is flagged for administrative action.
    ///
    /// Authorization: Admin can flag any review. HallManagers can only flag reviews for halls they manage.
    /// </remarks>
    /// <param name="id">The hall ID (for authorization)</param>
    /// <param name="reviewId">The review ID to flag</param>
    /// <param name="request">The flag reason</param>
    /// <response code="200">Review flagged successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="403">User does not have access to this hall</response>
    /// <response code="404">Review not found or does not belong to this hall</response>
    [HttpPost("{id:int}/reviews/{reviewId:int}/flag")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> FlagReview(
        int id, int reviewId, [FromBody] FlagReviewRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Error($"Invalid flag data: {errors}", 400);
            }

            // Verify hall exists
            var hall = await _hallQueryService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return Error("Hall not found", 404);
            }

            // Verify ownership
            if (!await UserOwnsHall(id))
            {
                return Error("You do not have permission to flag reviews for this hall", 403);
            }

            // Verify the review belongs to this hall
            var existingReview = await _reviewManagementService.GetReviewByIdAsync(reviewId);
            if (existingReview == null)
            {
                return Error("Review not found", 404);
            }

            if (existingReview.HallId != id)
            {
                return Error("Review does not belong to this hall", 400);
            }

            var success = await _reviewManagementService.FlagReviewAsync(
                reviewId, request.Reason, UserId);

            if (!success)
            {
                return Error("Failed to flag review", 500);
            }

            _logger.LogInformation(
                "Review {ReviewId} flagged by User {UserId} for Hall {HallId}",
                reviewId, UserId, id);

            return Success("Review flagged successfully for admin review");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flagging Review {ReviewId} for Hall {HallId}", reviewId, id);
            return Error("An error occurred while flagging the review. Please try again.", 500);
        }
    }
}
