using HallApp.Core.Exceptions;
using HallApp.Application.DTOs.Booking;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingStatusEnum = HallApp.Core.Enums.BookingStatus;
using ApprovalStatusEnum = HallApp.Core.Enums.ApprovalStatus;

namespace HallApp.Web.Controllers.Bookings;

/// <summary>
/// Controller for managing booking approval workflow (Hall Manager and Vendor approvals)
/// </summary>
[Route("api/bookings")]
public class BookingApprovalController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHallManagerService _hallManagerService;
    private readonly IVendorManagerService _vendorManagerService;
    private readonly IOrganizationService _organizationService;
    private readonly IHallService _hallService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BookingApprovalController> _logger;

    public BookingApprovalController(
        IUnitOfWork unitOfWork,
        IHallManagerService hallManagerService,
        IVendorManagerService vendorManagerService,
        IOrganizationService organizationService,
        IHallService hallService,
        INotificationService notificationService,
        ILogger<BookingApprovalController> logger)
    {
        _unitOfWork = unitOfWork;
        _hallManagerService = hallManagerService;
        _vendorManagerService = vendorManagerService;
        _organizationService = organizationService;
        _hallService = hallService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Helper method to check if the current HallManager or HallOrganizationManager user
    /// owns the hall associated with a booking.
    /// HallOrganizationManager: checks organization ownership via Organization entity,
    /// then falls back to direct hall assignments.
    /// HallManager: checks direct hall assignments via HallManager entity.
    /// </summary>
    private async Task<bool> CurrentUserOwnsBookingHall(int hallId)
    {
        if (!User.IsInRole("HallOrganizationManager") && !User.IsInRole("HallManager"))
        {
            return false;
        }

        // HallOrganizationManager: check org ownership of the hall
        if (User.IsInRole("HallOrganizationManager"))
        {
            var org = await _organizationService.GetOrganizationByOwnerId(UserId);
            if (org != null)
            {
                var orgHalls = await _hallService.GetOrganizationHallsAsync(org.Id);
                if (orgHalls?.Any(h => h.ID == hallId) == true)
                {
                    return true;
                }
            }
        }

        // HallManager (or fallback for HallOrganizationManager): check direct hall assignments
        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(UserId);
        if (hallManager?.Halls == null)
        {
            return false;
        }

        return hallManager.Halls.Any(h => h.ID == hallId);
    }

    /// <summary>
    /// Hall manager approves or rejects a booking
    /// </summary>
    [HttpPost("{bookingId}/hall-approval")]
    [Authorize(Roles = "HallOrganizationManager,HallManager,Admin")]
    public async Task<ActionResult<ApiResponse<ApprovalResponseDto>>> HallApproval(
        int bookingId,
        [FromBody] HallApprovalRequestDto request)
    {
        try
        {
            var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(bookingId);
            if (booking == null)
            {
                return Error<ApprovalResponseDto>("Booking not found", 404);
            }

            // Verify hall manager or organization manager owns this hall
            if (!IsAdmin)
            {
                var ownsHall = await CurrentUserOwnsBookingHall(booking.HallId);
                if (!ownsHall)
                {
                    _logger.LogWarning(
                        "Access denied: User {UserId} attempted to approve/reject booking {BookingId} for hall {HallId}",
                        UserId, bookingId, booking.HallId);
                    return Error<ApprovalResponseDto>("You do not have permission to approve bookings for this hall", 403);
                }
            }

            if (request.Approved)
            {
                booking.Status = BookingStatusEnum.HallApproved.ToString();

                // If there are vendor bookings, set status to VendorsApproving
                if (booking.VendorBookings != null && booking.VendorBookings.Any())
                {
                    booking.Status = BookingStatusEnum.VendorsApproving.ToString();

                    // Set all vendor bookings to pending
                    foreach (var vb in booking.VendorBookings)
                    {
                        vb.Status = ApprovalStatusEnum.Pending.ToString();
                    }
                }
                else
                {
                    // No vendors, can go straight to ready for payment
                    booking.Status = BookingStatusEnum.ReadyForPayment.ToString();
                }

                await _unitOfWork.Complete();

                return Success(new ApprovalResponseDto
                {
                    Success = true,
                    Message = "Booking approved successfully",
                    NewStatus = booking.Status,
                    CanProceedToPayment = booking.VendorBookings == null || !booking.VendorBookings.Any()
                }, "Hall approval successful");
            }
            else
            {
                booking.Status = BookingStatusEnum.HallRejected.ToString();
                booking.Comments = $"Hall Rejection: {request.RejectionReason ?? "No reason provided"}";

                await _unitOfWork.Complete();

                return Success(new ApprovalResponseDto
                {
                    Success = true,
                    Message = "Booking rejected",
                    NewStatus = booking.Status,
                    CanProceedToPayment = false
                }, "Hall rejection recorded");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing hall approval for booking {BookingId}", bookingId);
            return Error<ApprovalResponseDto>("An error occurred processing your request. Please try again.", 500);
        }
    }

    /// <summary>
    /// Vendor manager approves or rejects their service
    /// </summary>
    [HttpPost("{bookingId}/vendor-bookings/{vendorBookingId}/approval")]
    [Authorize(Roles = "VendorOrganizationManager,VendorManager,Admin")]
    public async Task<ActionResult<ApiResponse<ApprovalResponseDto>>> VendorApproval(
        int bookingId,
        int vendorBookingId,
        [FromBody] VendorApprovalRequestDto request)
    {
        try
        {
            var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(bookingId);
            if (booking == null)
            {
                return Error<ApprovalResponseDto>("Booking not found", 404);
            }

            var vendorBooking = booking.VendorBookings?.FirstOrDefault(vb => vb.Id == vendorBookingId);
            if (vendorBooking == null)
            {
                return Error<ApprovalResponseDto>("Vendor booking not found", 404);
            }

            // Verify vendor manager owns this vendor
            if (!IsAdmin)
            {
                var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(UserId);
                if (vendorManager == null || !vendorManager.Vendors.Any(v => v.Id == vendorBooking.VendorId))
                {
                    return Error<ApprovalResponseDto>("You do not have permission to approve services for this vendor", 403);
                }
            }

            if (request.Approved)
            {
                vendorBooking.Status = ApprovalStatusEnum.Approved.ToString();
            }
            else
            {
                vendorBooking.Status = ApprovalStatusEnum.Rejected.ToString();
                // Note: RejectionReason stored in Comments for now
                // TODO: Add RejectionReason to VendorBooking entity
            }

            // Check if all vendors have responded
            var allVendorsResponded = booking.VendorBookings!
                .All(vb => vb.Status == ApprovalStatusEnum.Approved.ToString() ||
                          vb.Status == ApprovalStatusEnum.Rejected.ToString());

            if (allVendorsResponded)
            {
                var allApproved = booking.VendorBookings!
                    .All(vb => vb.Status == ApprovalStatusEnum.Approved.ToString());

                var allRejected = booking.VendorBookings!
                    .All(vb => vb.Status == ApprovalStatusEnum.Rejected.ToString());

                var hasApprovals = booking.VendorBookings!
                    .Any(vb => vb.Status == ApprovalStatusEnum.Approved.ToString());

                if (allApproved)
                {
                    // All vendors approved - proceed to payment
                    booking.Status = BookingStatusEnum.ReadyForPayment.ToString();
                }
                else if (allRejected)
                {
                    // All vendors rejected - mark as rejected
                    booking.Status = BookingStatusEnum.VendorRejected.ToString();
                }
                else if (hasApprovals)
                {
                    // Mixed: some approved, some rejected
                    // Allow customer to proceed with approved vendors only
                    booking.Status = BookingStatusEnum.ReadyForPayment.ToString();

                    var rejectedVendors = booking.VendorBookings!
                        .Where(vb => vb.Status == ApprovalStatusEnum.Rejected.ToString())
                        .Select(vb => vb.Vendor?.Name ?? "Unknown")
                        .ToList();

                    var approvedVendors = booking.VendorBookings!
                        .Where(vb => vb.Status == ApprovalStatusEnum.Approved.ToString())
                        .Select(vb => vb.Vendor?.Name ?? "Unknown")
                        .ToList();

                    var noteMessage = $"Partial vendor approval: {rejectedVendors.Count} vendor(s) rejected ({string.Join(", ", rejectedVendors)}). Proceeding with {approvedVendors.Count} approved vendor(s).";

                    booking.Comments = string.IsNullOrEmpty(booking.Comments)
                        ? noteMessage
                        : $"{booking.Comments} | {noteMessage}";

                    _logger.LogInformation(
                        "Booking {BookingId} has partial vendor approval. Approved: {ApprovedVendors}. Rejected: {RejectedVendors}",
                        bookingId, string.Join(", ", approvedVendors), string.Join(", ", rejectedVendors));

                    // Notify the customer about the partial approval
                    await NotifyCustomerPartialApprovalAsync(
                        booking, approvedVendors, rejectedVendors);
                }
            }

            await _unitOfWork.Complete();

            var isPartialApproval = booking.Status == BookingStatusEnum.ReadyForPayment.ToString()
                && booking.VendorBookings!.Any(vb => vb.Status == ApprovalStatusEnum.Rejected.ToString());

            return Success(new ApprovalResponseDto
            {
                Success = true,
                Message = isPartialApproval
                    ? "Vendor service rejected. Booking can proceed with approved vendors."
                    : request.Approved ? "Vendor service approved" : "Vendor service rejected",
                NewStatus = booking.Status,
                CanProceedToPayment = booking.Status == BookingStatusEnum.ReadyForPayment.ToString()
            }, "Vendor approval processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing vendor approval for booking {BookingId}, vendor booking {VendorBookingId}", bookingId, vendorBookingId);
            return Error<ApprovalResponseDto>("An error occurred processing your request. Please try again.", 500);
        }
    }

    /// <summary>
    /// Get vendor approval status for a booking.
    /// Authorization: Admin, booking customer, hall manager/org manager for the hall,
    /// or vendor manager/org manager for a vendor on the booking.
    /// </summary>
    [HttpGet("{bookingId}/vendor-approval-status")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<VendorApprovalStatusDto>>> GetVendorApprovalStatus(int bookingId)
    {
        try
        {
            var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(bookingId);
            if (booking == null)
            {
                return Error<VendorApprovalStatusDto>("Booking not found", 404);
            }

            // RBAC-001: Authorization check - verify caller has access to this booking
            var isAuthorized = IsAdmin;

            if (!isAuthorized)
            {
                // Check if customer who owns the booking
                var customer = await _unitOfWork.CustomerRepository.GetCustomerByAppUserIdAsync(UserId);
                if (customer != null && booking.CustomerId == customer.Id)
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                // Check if hall manager or hall organization manager for this booking's hall
                if (User.IsInRole("HallManager") || User.IsInRole("HallOrganizationManager"))
                {
                    isAuthorized = await CurrentUserOwnsBookingHall(booking.HallId);
                }
            }

            if (!isAuthorized)
            {
                // Check if vendor manager or vendor organization manager for a vendor on this booking
                if (User.IsInRole("VendorManager") || User.IsInRole("VendorOrganizationManager"))
                {
                    var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(UserId);
                    if (vendorManager?.Vendors != null && booking.VendorBookings != null)
                    {
                        isAuthorized = booking.VendorBookings
                            .Any(vb => vendorManager.Vendors.Any(v => v.Id == vb.VendorId));
                    }
                }
            }

            if (!isAuthorized)
            {
                _logger.LogWarning(
                    "RBAC-001: User {UserId} unauthorized to view vendor approval status for booking {BookingId}",
                    UserId, bookingId);
                return Error<VendorApprovalStatusDto>("You do not have permission to view this booking's approval status", 403);
            }

            var vendorBookings = booking.VendorBookings ?? new List<Core.Entities.VendorEntities.VendorBooking>();

            var approvedCount = vendorBookings.Count(vb => vb.Status == ApprovalStatusEnum.Approved.ToString());
            var rejectedCount = vendorBookings.Count(vb => vb.Status == ApprovalStatusEnum.Rejected.ToString());
            var pendingCount = vendorBookings.Count(vb => vb.Status == ApprovalStatusEnum.Pending.ToString() || string.IsNullOrEmpty(vb.Status));

            var allApproved = vendorBookings.Any() && vendorBookings.All(vb => vb.Status == ApprovalStatusEnum.Approved.ToString());
            var allRejected = vendorBookings.Any() && vendorBookings.All(vb => vb.Status == ApprovalStatusEnum.Rejected.ToString());
            var hasApprovals = vendorBookings.Any(vb => vb.Status == ApprovalStatusEnum.Approved.ToString());
            var isPartialApproval = hasApprovals && rejectedCount > 0 && pendingCount == 0;

            // Partial approvals (some approved, some rejected) can proceed to payment
            var canProceedToPayment = !vendorBookings.Any() || allApproved || (hasApprovals && pendingCount == 0);

            var rejectedVendorNames = vendorBookings
                .Where(vb => vb.Status == ApprovalStatusEnum.Rejected.ToString())
                .Select(vb => vb.Vendor?.Name ?? "Unknown")
                .ToList();

            var status = new VendorApprovalStatusDto
            {
                TotalVendors = vendorBookings.Count,
                ApprovedCount = approvedCount,
                RejectedCount = rejectedCount,
                PendingCount = pendingCount,
                AllApproved = allApproved,
                CanProceedToPayment = canProceedToPayment,
                IsPartialApproval = isPartialApproval,
                RejectedVendorNames = rejectedVendorNames
            };

            return Success(status, "Vendor approval status retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor approval status for booking {BookingId}", bookingId);
            return Error<VendorApprovalStatusDto>("An error occurred processing your request. Please try again.", 500);
        }
    }

    /// <summary>
    /// Sends a notification to the customer explaining which vendors rejected and that
    /// they can proceed with approved vendors or choose to replace the rejected ones.
    /// </summary>
    private async Task NotifyCustomerPartialApprovalAsync(
        Core.Entities.BookingEntities.Booking booking,
        List<string> approvedVendors,
        List<string> rejectedVendors)
    {
        try
        {
            var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(booking.CustomerId);
            if (customer == null)
            {
                _logger.LogWarning(
                    "Cannot notify customer for booking {BookingId} - customer {CustomerId} not found",
                    booking.Id, booking.CustomerId);
                return;
            }

            var message =
                $"Your booking #{booking.Id} has received partial vendor approval. " +
                $"{approvedVendors.Count} vendor(s) approved ({string.Join(", ", approvedVendors)}). " +
                $"{rejectedVendors.Count} vendor(s) rejected ({string.Join(", ", rejectedVendors)}). " +
                "You can proceed to payment with the approved vendors, or you may choose to replace the rejected vendors before continuing.";

            await _notificationService.SendBookingNotificationAsync(
                customer.AppUserId,
                booking.Id.ToString(),
                booking.Status,
                message);

            _logger.LogInformation(
                "Partial approval notification sent to customer {CustomerId} (AppUserId: {AppUserId}) for booking {BookingId}",
                booking.CustomerId, customer.AppUserId, booking.Id);
        }
        catch (Exception ex)
        {
            // Notification failure should not block the approval workflow
            _logger.LogError(ex,
                "Failed to send partial approval notification for booking {BookingId} to customer {CustomerId}",
                booking.Id, booking.CustomerId);
        }
    }
}
