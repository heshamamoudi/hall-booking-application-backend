using HallApp.Application.Common.Models;
using HallApp.Application.DTOs.Booking;
using HallApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingStatusEnum = HallApp.Core.Enums.BookingStatus;
using ApprovalStatusEnum = HallApp.Core.Enums.ApprovalStatus;

namespace HallApp.Web.Controllers.Bookings;

[Route("api/bookings")]
[ApiController]
public class BookingApprovalController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public BookingApprovalController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Hall manager approves or rejects a booking
    /// </summary>
    [HttpPost("{bookingId}/hall-approval")]
    [Authorize(Roles = "HallManager,Admin")]
    public async Task<ActionResult<ApiResponse<ApprovalResponseDto>>> HallApproval(
        int bookingId,
        [FromBody] HallApprovalRequestDto request)
    {
        try
        {
            var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(bookingId);
            if (booking == null)
            {
                return NotFound(new ApiResponse<ApprovalResponseDto> 
                { 
                    IsSuccess = false, 
                    Message = "Booking not found",
                    StatusCode = 404
                });
            }

            // Verify hall manager owns this hall
            // TODO: Add proper authorization check

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

                return Ok(new ApiResponse<ApprovalResponseDto>
                {
                    IsSuccess = true,
                    Message = "Hall approval successful",
                    Data = new ApprovalResponseDto
                    {
                        Success = true,
                        Message = "Booking approved successfully",
                        NewStatus = booking.Status,
                        CanProceedToPayment = booking.VendorBookings == null || !booking.VendorBookings.Any()
                    }
                });
            }
            else
            {
                booking.Status = BookingStatusEnum.HallRejected.ToString();
                booking.Comments = $"Hall Rejection: {request.RejectionReason ?? "No reason provided"}";
                
                await _unitOfWork.Complete();

                return Ok(new ApiResponse<ApprovalResponseDto>
                {
                    IsSuccess = true,
                    Message = "Hall rejection recorded",
                    Data = new ApprovalResponseDto
                    {
                        Success = true,
                        Message = "Booking rejected",
                        NewStatus = booking.Status,
                        CanProceedToPayment = false
                    }
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<ApprovalResponseDto> 
            { 
                IsSuccess = false, 
                StatusCode = 500,
                Message = $"Error processing hall approval: {ex.Message}" 
            });
        }
    }

    /// <summary>
    /// Vendor manager approves or rejects their service
    /// </summary>
    [HttpPost("{bookingId}/vendor-bookings/{vendorBookingId}/approval")]
    [Authorize(Roles = "VendorManager,Admin")]
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
                return NotFound(new ApiResponse<ApprovalResponseDto> 
                { 
                    IsSuccess = false, 
                    Message = "Booking not found",
                    StatusCode = 404
                });
            }

            var vendorBooking = booking.VendorBookings?.FirstOrDefault(vb => vb.Id == vendorBookingId);
            if (vendorBooking == null)
            {
                return NotFound(new ApiResponse<ApprovalResponseDto> 
                { 
                    IsSuccess = false, 
                    Message = "Vendor booking not found",
                    StatusCode = 404
                });
            }

            // TODO: Verify vendor manager owns this vendor

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
                var allApproved = booking.VendorBookings
                    .All(vb => vb.Status == ApprovalStatusEnum.Approved.ToString());
                
                var allRejected = booking.VendorBookings
                    .All(vb => vb.Status == ApprovalStatusEnum.Rejected.ToString());

                if (allApproved || allRejected)
                {
                    booking.Status = BookingStatusEnum.ReadyForPayment.ToString();
                }
                else
                {
                    // Some approved, some rejected
                    booking.Status = BookingStatusEnum.VendorRejected.ToString();
                }
            }

            await _unitOfWork.Complete();

            return Ok(new ApiResponse<ApprovalResponseDto>
            {
                IsSuccess = true,
                Message = "Vendor approval processed",
                Data = new ApprovalResponseDto
                {
                    Success = true,
                    Message = request.Approved ? "Vendor service approved" : "Vendor service rejected",
                    NewStatus = booking.Status,
                    CanProceedToPayment = booking.Status == BookingStatusEnum.ReadyForPayment.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<ApprovalResponseDto> 
            { 
                IsSuccess = false, 
                StatusCode = 500,
                Message = $"Error processing vendor approval: {ex.Message}" 
            });
        }
    }

    /// <summary>
    /// Get vendor approval status for a booking
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
                return NotFound(new ApiResponse<VendorApprovalStatusDto> 
                { 
                    IsSuccess = false, 
                    Message = "Booking not found",
                    StatusCode = 404
                });
            }

            var vendorBookings = booking.VendorBookings ?? new List<Core.Entities.VendorEntities.VendorBooking>();
            
            var approvedCount = vendorBookings.Count(vb => vb.Status == ApprovalStatusEnum.Approved.ToString());
            var rejectedCount = vendorBookings.Count(vb => vb.Status == ApprovalStatusEnum.Rejected.ToString());
            var pendingCount = vendorBookings.Count(vb => vb.Status == ApprovalStatusEnum.Pending.ToString() || string.IsNullOrEmpty(vb.Status));

            var allApproved = vendorBookings.Any() && vendorBookings.All(vb => vb.Status == ApprovalStatusEnum.Approved.ToString());
            var allRejected = vendorBookings.Any() && vendorBookings.All(vb => vb.Status == ApprovalStatusEnum.Rejected.ToString());
            var canProceedToPayment = !vendorBookings.Any() || allApproved || allRejected;

            var status = new VendorApprovalStatusDto
            {
                TotalVendors = vendorBookings.Count,
                ApprovedCount = approvedCount,
                RejectedCount = rejectedCount,
                PendingCount = pendingCount,
                AllApproved = allApproved,
                CanProceedToPayment = canProceedToPayment
            };

            return Ok(new ApiResponse<VendorApprovalStatusDto>
            {
                IsSuccess = true,
                Message = "Vendor approval status retrieved",
                Data = status
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<VendorApprovalStatusDto> 
            { 
                IsSuccess = false, 
                StatusCode = 500,
                Message = $"Error retrieving vendor approval status: {ex.Message}" 
            });
        }
    }
}
