using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HallApp.Application.DTOs.Halls.Availability;
using HallApp.Application.Services;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using System.Security.Claims;

namespace HallApp.Web.Controllers.Hall;

/// <summary>
/// Hall availability and blocked dates management endpoints.
/// Allows hall managers to block specific dates to prevent bookings.
/// Requires HallManager or Admin role. HallManagers can only manage their own halls.
/// </summary>
[Authorize(Roles = "Admin,HallOrganizationManager,HallManager")]
[Route("api/halls")]
[ApiController]
public class HallAvailabilityController : BaseApiController
{
    private readonly IHallBlockedDateService _availabilityService;
    private readonly IHallQueryService _hallQueryService;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<HallAvailabilityController> _logger;

    public HallAvailabilityController(
        IHallBlockedDateService availabilityService,
        IHallQueryService hallQueryService,
        IOrganizationService organizationService,
        ILogger<HallAvailabilityController> logger)
    {
        _availabilityService = availabilityService;
        _hallQueryService = hallQueryService;
        _organizationService = organizationService;
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
    /// Get all active blocked dates for a specific hall
    /// </summary>
    /// <remarks>
    /// Returns a list of all currently active (not removed) blocked date ranges for the hall.
    /// Each entry includes the date range, reason, and who created the block.
    ///
    /// Authorization: Admin can view any hall. HallManagers can only view halls they manage.
    /// </remarks>
    /// <param name="id">The hall ID</param>
    /// <response code="200">Returns the list of blocked dates</response>
    /// <response code="403">User does not have access to this hall</response>
    /// <response code="404">Hall not found</response>
    [HttpGet("{id:int}/availability/blocked-dates")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BlockedDateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IEnumerable<BlockedDateDto>>>> GetBlockedDates(int id)
    {
        try
        {
            // Verify hall exists
            var hall = await _hallQueryService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return Error<IEnumerable<BlockedDateDto>>("Hall not found", 404);
            }

            // Verify ownership
            if (!await UserOwnsHall(id))
            {
                return Error<IEnumerable<BlockedDateDto>>(
                    "You do not have permission to view blocked dates for this hall", 403);
            }

            var blockedDates = await _availabilityService.GetBlockedDatesAsync(id);
            return Success(blockedDates, "Blocked dates retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blocked dates for Hall {HallId}", id);
            return Error<IEnumerable<BlockedDateDto>>(
                "An error occurred while loading blocked dates. Please try again.", 500);
        }
    }

    /// <summary>
    /// Block a date range for a hall
    /// </summary>
    /// <remarks>
    /// Blocks the specified date range to prevent new bookings.
    ///
    /// Validation:
    /// - StartDate must be today or in the future
    /// - EndDate must be >= StartDate
    /// - Reason is required (1-500 characters)
    /// - Cannot block dates that have confirmed or pending bookings
    ///
    /// Authorization: Admin can block any hall. HallManagers can only block halls they manage.
    /// </remarks>
    /// <param name="id">The hall ID</param>
    /// <param name="request">Block date request containing date range and reason</param>
    /// <response code="201">Dates successfully blocked</response>
    /// <response code="400">Invalid request or validation failed</response>
    /// <response code="403">User does not have access to this hall</response>
    /// <response code="404">Hall not found</response>
    /// <response code="409">Booking conflicts exist in the specified date range</response>
    [HttpPost("{id:int}/availability/block")]
    [ProducesResponseType(typeof(ApiResponse<BlockedDateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BlockedDateDto>>> BlockDates(
        int id, [FromBody] BlockDateRequestDto request)
    {
        try
        {
            // Verify hall exists
            var hall = await _hallQueryService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return Error<BlockedDateDto>("Hall not found", 404);
            }

            // Verify ownership
            if (!await UserOwnsHall(id))
            {
                return Error<BlockedDateDto>(
                    "You do not have permission to block dates for this hall", 403);
            }

            // Get current user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Error<BlockedDateDto>("User not authenticated", 401);
            }

            // Attempt to block dates
            var blockedDate = await _availabilityService.BlockDatesAsync(id, request, userId);

            return StatusCode(201, new ApiResponse<BlockedDateDto>
            {
                StatusCode = 201,
                Message = "Dates blocked successfully",
                Data = blockedDate,
                IsSuccess = true
            });
        }
        catch (InvalidOperationException ex)
        {
            // Business logic error (e.g., booking conflicts)
            _logger.LogWarning(ex, "Cannot block dates for Hall {HallId}: {Message}", id, ex.Message);
            return Error<BlockedDateDto>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking dates for Hall {HallId}", id);
            return Error<BlockedDateDto>(
                "An error occurred while blocking dates. Please try again.", 500);
        }
    }

    /// <summary>
    /// Unblock (remove) a blocked date range
    /// </summary>
    /// <remarks>
    /// Removes a previously blocked date range, allowing bookings again.
    /// This is a soft delete operation - the record remains in the database but is marked as inactive.
    ///
    /// Authorization: Admin can unblock any hall. HallManagers can only unblock halls they manage.
    /// </remarks>
    /// <param name="id">The hall ID</param>
    /// <param name="blockId">The blocked date ID to remove</param>
    /// <response code="200">Dates successfully unblocked</response>
    /// <response code="403">User does not have access to this hall</response>
    /// <response code="404">Hall or blocked date not found</response>
    [HttpDelete("{id:int}/availability/blocked-dates/{blockId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> UnblockDates(int id, int blockId)
    {
        try
        {
            // Verify hall exists
            var hall = await _hallQueryService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return Error("Hall not found", 404);
            }

            // Verify ownership
            if (!await UserOwnsHall(id))
            {
                return Error("You do not have permission to unblock dates for this hall", 403);
            }

            // Attempt to unblock
            var success = await _availabilityService.UnblockDatesAsync(id, blockId);

            if (!success)
            {
                return Error("Blocked date not found or already removed", 404);
            }

            return Success("Dates unblocked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking dates for Hall {HallId}, Block {BlockId}", id, blockId);
            return Error("An error occurred while unblocking dates. Please try again.", 500);
        }
    }

    /// <summary>
    /// Get calendar availability for a specific month
    /// </summary>
    /// <remarks>
    /// Returns both blocked dates and existing bookings for the specified month.
    /// This provides all data needed to render a calendar view showing availability.
    ///
    /// Booked dates exclude cancelled/rejected bookings.
    /// Blocked dates only include active (not removed) blocks.
    ///
    /// Authorization: Admin can view any hall. HallManagers can only view halls they manage.
    /// </remarks>
    /// <param name="id">The hall ID</param>
    /// <param name="year">Year to query (e.g., 2024)</param>
    /// <param name="month">Month to query (1-12)</param>
    /// <response code="200">Returns calendar availability data</response>
    /// <response code="400">Invalid month or year</response>
    /// <response code="403">User does not have access to this hall</response>
    /// <response code="404">Hall not found</response>
    [HttpGet("{id:int}/availability/calendar")]
    [ProducesResponseType(typeof(ApiResponse<CalendarAvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CalendarAvailabilityDto>>> GetCalendarAvailability(
        int id, [FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            // Validate month and year
            if (month < 1 || month > 12)
            {
                return Error<CalendarAvailabilityDto>("Month must be between 1 and 12", 400);
            }

            if (year < 2020 || year > 2100)
            {
                return Error<CalendarAvailabilityDto>("Year must be between 2020 and 2100", 400);
            }

            // Verify hall exists
            var hall = await _hallQueryService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return Error<CalendarAvailabilityDto>("Hall not found", 404);
            }

            // Verify ownership
            if (!await UserOwnsHall(id))
            {
                return Error<CalendarAvailabilityDto>(
                    "You do not have permission to view calendar for this hall", 403);
            }

            var availability = await _availabilityService.GetCalendarAvailabilityAsync(id, year, month);
            return Success(availability, $"Calendar availability retrieved for {year}-{month:D2}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving calendar for Hall {HallId}, Year {Year}, Month {Month}",
                id, year, month);
            return Error<CalendarAvailabilityDto>(
                "An error occurred while loading calendar data. Please try again.", 500);
        }
    }
}
