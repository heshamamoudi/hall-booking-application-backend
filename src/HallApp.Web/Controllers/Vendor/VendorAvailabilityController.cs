using AutoMapper;
using HallApp.Application.DTOs.Vendors;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Vendor;

/// <summary>
/// When a vendor is open, when it is blocked out, and whether it can take a job.
///
/// IVendorAvailabilityService already implemented all of this - business hours,
/// blocked dates, slot search - but it was never registered in DI and no
/// controller referenced it, so 63 seeded business-hour rows were unreachable and
/// every availability question answered "yes". This exposes it.
///
/// Reads are public: a customer choosing a vendor needs to see opening hours, the
/// same way hall availability is public. Writes are restricted to whoever manages
/// the vendor.
/// </summary>
[Route("api/vendors")]
[ApiController]
public class VendorAvailabilityController : BaseApiController
{
    private readonly IVendorAvailabilityService _availabilityService;
    private readonly IVendorService _vendorService;
    private readonly IOrganizationService _organizationService;
    private readonly IMapper _mapper;
    private readonly ILogger<VendorAvailabilityController> _logger;

    public VendorAvailabilityController(
        IVendorAvailabilityService availabilityService,
        IVendorService vendorService,
        IOrganizationService organizationService,
        IMapper mapper,
        ILogger<VendorAvailabilityController> logger)
    {
        _availabilityService = availabilityService;
        _vendorService = vendorService;
        _organizationService = organizationService;
        _mapper = mapper;
        _logger = logger;
    }

    // ===================================================================
    // Business hours
    // ===================================================================

    /// <summary>The vendor's weekly opening hours.</summary>
    [AllowAnonymous]
    [HttpGet("{vendorId:int}/business-hours")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorBusinessHourDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<VendorBusinessHourDto>>>> GetBusinessHours(int vendorId)
    {
        try
        {
            var hours = await _availabilityService.GetBusinessHoursAsync(vendorId);
            var dtos = _mapper.Map<List<VendorBusinessHourDto>>(hours);
            return Success(dtos, $"Found business hours for {dtos.Count} day(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading business hours for vendor {VendorId}", vendorId);
            return Error<List<VendorBusinessHourDto>>("An error occurred while reading business hours", 500);
        }
    }

    /// <summary>
    /// Replace the hours for one day of the week. Creates the entry if the vendor
    /// has never set that day, so a caller does not have to know which it is.
    /// </summary>
    [Authorize(Roles = "Admin,VendorOrganizationManager,VendorManager")]
    [HttpPut("{vendorId:int}/business-hours/{dayOfWeek}")]
    [ProducesResponseType(typeof(ApiResponse<VendorBusinessHourDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<VendorBusinessHourDto>>> SetBusinessHour(
        int vendorId, DayOfWeek dayOfWeek, [FromBody] CreateVendorBusinessHourDto dto)
    {
        try
        {
            if (!await UserOwnsVendor(vendorId))
                return Error<VendorBusinessHourDto>("You do not have permission to manage this vendor", 403);

            if (!dto.IsClosed && dto.CloseTime <= dto.OpenTime)
                return Error<VendorBusinessHourDto>("CloseTime must be later than OpenTime", 400);

            // Mutate the tracked row when one exists. Handing the update a freshly
            // constructed entity leaves Id at 0, so EF treats it as an insert and
            // the unique (VendorId, DayOfWeek) index rejects it.
            var existing = await _availabilityService.GetBusinessHoursAsync(vendorId);
            var current = existing.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

            VendorBusinessHour saved;
            if (current != null)
            {
                current.OpenTime = dto.OpenTime;
                current.CloseTime = dto.CloseTime;
                current.IsClosed = dto.IsClosed;
                current.SpecialNote = dto.SpecialNote ?? string.Empty;
                saved = await _availabilityService.UpdateBusinessHourAsync(vendorId, dayOfWeek, current);
            }
            else
            {
                saved = await _availabilityService.AddBusinessHourAsync(vendorId, new VendorBusinessHour
                {
                    VendorId = vendorId,
                    DayOfWeek = dayOfWeek,
                    OpenTime = dto.OpenTime,
                    CloseTime = dto.CloseTime,
                    IsClosed = dto.IsClosed,
                    SpecialNote = dto.SpecialNote ?? string.Empty
                });
            }

            return Success(_mapper.Map<VendorBusinessHourDto>(saved), "Business hours saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving business hours for vendor {VendorId}", vendorId);
            return Error<VendorBusinessHourDto>("An error occurred while saving business hours", 500);
        }
    }

    /// <summary>Remove the entry for one day, leaving it unset.</summary>
    [Authorize(Roles = "Admin,VendorOrganizationManager,VendorManager")]
    [HttpDelete("{vendorId:int}/business-hours/{dayOfWeek}")]
    public async Task<ActionResult<ApiResponse>> DeleteBusinessHour(int vendorId, DayOfWeek dayOfWeek)
    {
        try
        {
            if (!await UserOwnsVendor(vendorId))
                return Error("You do not have permission to manage this vendor", 403);

            await _availabilityService.DeleteBusinessHourAsync(vendorId, dayOfWeek);
            return Success("Business hours removed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business hours for vendor {VendorId}", vendorId);
            return Error("An error occurred while deleting business hours", 500);
        }
    }

    // ===================================================================
    // Blocked dates
    // ===================================================================

    /// <summary>Dates the vendor has blocked out, optionally within a range.</summary>
    [AllowAnonymous]
    [HttpGet("{vendorId:int}/blocked-dates")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorBlockedDate>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<VendorBlockedDate>>>> GetBlockedDates(
        int vendorId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        try
        {
            var blocked = from.HasValue && to.HasValue
                ? await _availabilityService.GetBlockedDatesAsync(vendorId, AsUtc(from.Value), AsUtc(to.Value))
                : await _availabilityService.GetBlockedDatesAsync(vendorId);

            return Success(blocked, $"Found {blocked.Count} blocked period(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading blocked dates for vendor {VendorId}", vendorId);
            return Error<List<VendorBlockedDate>>("An error occurred while reading blocked dates", 500);
        }
    }

    /// <summary>Block a date range - holidays, maintenance, a private booking.</summary>
    [Authorize(Roles = "Admin,VendorOrganizationManager,VendorManager")]
    [HttpPost("{vendorId:int}/blocked-dates")]
    [ProducesResponseType(typeof(ApiResponse<VendorBlockedDate>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<VendorBlockedDate>>> AddBlockedDate(
        int vendorId, [FromBody] VendorBlockedDateDto dto)
    {
        try
        {
            if (!await UserOwnsVendor(vendorId))
                return Error<VendorBlockedDate>("You do not have permission to manage this vendor", 403);

            if (dto.EndDate < dto.StartDate)
                return Error<VendorBlockedDate>("EndDate cannot be before StartDate", 400);

            // The route decides which vendor this belongs to, not the body.
            var blockedDate = new VendorBlockedDate
            {
                VendorId = vendorId,
                StartDate = AsUtc(dto.StartDate),
                EndDate = AsUtc(dto.EndDate),
                Reason = dto.Reason ?? string.Empty
            };

            var created = await _availabilityService.AddBlockedDateAsync(vendorId, blockedDate);

            return StatusCode(201, new ApiResponse<VendorBlockedDate>
            {
                StatusCode = 201,
                Message = "Blocked period added",
                IsSuccess = true,
                Data = created
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding blocked date for vendor {VendorId}", vendorId);
            return Error<VendorBlockedDate>("An error occurred while adding the blocked period", 500);
        }
    }

    /// <summary>Remove a blocked period.</summary>
    [Authorize(Roles = "Admin,VendorOrganizationManager,VendorManager")]
    [HttpDelete("{vendorId:int}/blocked-dates/{blockedDateId:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteBlockedDate(int vendorId, int blockedDateId)
    {
        try
        {
            if (!await UserOwnsVendor(vendorId))
                return Error("You do not have permission to manage this vendor", 403);

            await _availabilityService.DeleteBlockedDateAsync(vendorId, blockedDateId);
            return Success("Blocked period removed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blocked date {BlockedDateId}", blockedDateId);
            return Error("An error occurred while removing the blocked period", 500);
        }
    }

    // ===================================================================
    // Availability
    // ===================================================================

    /// <summary>
    /// Whether the vendor can take work on a date, optionally within a time window.
    /// Considers business hours, blocked dates and existing bookings.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{vendorId:int}/availability")]
    [ProducesResponseType(typeof(ApiResponse<VendorAvailabilityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VendorAvailabilityDto>>> CheckAvailability(
        int vendorId,
        [FromQuery] DateTime date,
        [FromQuery] TimeSpan? startTime = null,
        [FromQuery] TimeSpan? endTime = null)
    {
        try
        {
            date = AsUtc(date);

            var available = startTime.HasValue && endTime.HasValue
                ? await _availabilityService.IsVendorAvailableAsync(vendorId, date, startTime.Value, endTime.Value)
                : await _availabilityService.IsVendorAvailableAsync(vendorId, date);

            return Success(new VendorAvailabilityDto
            {
                VendorId = vendorId,
                Date = date.Date,
                StartTime = startTime,
                EndTime = endTime,
                IsAvailable = available
            }, available ? "Vendor is available" : "Vendor is not available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for vendor {VendorId}", vendorId);
            return Error<VendorAvailabilityDto>("An error occurred while checking availability", 500);
        }
    }

    /// <summary>Free start times on a date for a job of the given length.</summary>
    [AllowAnonymous]
    [HttpGet("{vendorId:int}/availability/slots")]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSpan>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TimeSpan>>>> GetAvailableSlots(
        int vendorId, [FromQuery] DateTime date, [FromQuery] int durationMinutes = 120)
    {
        try
        {
            if (durationMinutes <= 0)
                return Error<List<TimeSpan>>("durationMinutes must be positive", 400);

            var slots = await _availabilityService.GetAvailableTimeSlotsAsync(vendorId, AsUtc(date), durationMinutes);
            return Success(slots, $"Found {slots.Count} available slot(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading slots for vendor {VendorId}", vendorId);
            return Error<List<TimeSpan>>("An error occurred while reading available slots", 500);
        }
    }

    // ===================================================================
    // Authorization
    // ===================================================================

    private async Task<bool> UserOwnsVendor(int vendorId)
    {
        if (IsAdmin) return true;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return false;

        var assigned = await _vendorService.GetVendorsByManagerIdAsync(userId);
        if (assigned.Any(v => v.Id == vendorId))
            return true;

        if (User.IsInRole("VendorOrganizationManager"))
        {
            var organization = await _organizationService.GetOrganizationByOwnerId(UserId);
            if (organization != null && organization.Type == "VendorManagement")
            {
                var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
                if (vendor != null && vendor.OrganizationId == organization.Id)
                    return true;
            }
        }

        return false;
    }
}
