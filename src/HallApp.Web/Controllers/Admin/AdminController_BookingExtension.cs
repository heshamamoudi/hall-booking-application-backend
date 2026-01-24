using HallApp.Web.Controllers.Common;
using HallApp.Core.Exceptions;
using HallApp.Application.DTOs.Booking;
using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;

namespace HallApp.Web.Controllers.Admin
{
    /// <summary>
    /// Booking Management Extension for AdminController
    /// Provides full booking oversight and management for Admins
    /// </summary>
    public partial class AdminController
    {
        // Booking Management Methods

        /// <summary>
        /// Get all bookings (Admin view with full details)
        /// </summary>
        /// <returns>List of all bookings</returns>
        [HttpGet("bookings")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetAllBookingsAdmin()
        {
            try
            {
                var bookings = await _bookingService.GetAllBookingsAsync();
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);
                return Success(bookingDtos, "All bookings retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<BookingDto>>($"Failed to retrieve bookings: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get booking by ID (Admin view)
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Booking details</returns>
        [HttpGet("bookings/{id:int}")]
        public async Task<ActionResult<ApiResponse<BookingDto>>> GetBookingByIdAdmin(int id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);
                if (booking == null)
                {
                    return Error<BookingDto>($"Booking with ID {id} not found", 404);
                }

                var bookingDto = _mapper.Map<BookingDto>(booking);
                return Success(bookingDto, "Booking retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<BookingDto>($"Failed to retrieve booking: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update booking status (Admin override)
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="status">New status</param>
        /// <returns>Updated booking</returns>
        [HttpPut("bookings/{id:int}/status")]
        public async Task<ActionResult<ApiResponse<BookingDto>>> UpdateBookingStatusAdmin(int id, [FromBody] string status)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);
                if (booking == null)
                {
                    return Error<BookingDto>($"Booking with ID {id} not found", 404);
                }

                booking.Status = status;
                var updatedBooking = await _bookingService.UpdateBookingAsync(booking);
                var bookingDto = _mapper.Map<BookingDto>(updatedBooking);

                return Success(bookingDto, $"Booking status updated to {status}");
            }
            catch (Exception ex)
            {
                return Error<BookingDto>($"Failed to update booking status: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Force approve booking (Admin override - bypasses normal approval workflow)
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Approved booking</returns>
        [HttpPost("bookings/{id:int}/force-approve")]
        public async Task<ActionResult<ApiResponse<BookingDto>>> ForceApproveBooking(int id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);
                if (booking == null)
                {
                    return Error<BookingDto>($"Booking with ID {id} not found", 404);
                }

                // Admin override - mark as confirmed regardless of vendor approvals
                booking.Status = "Confirmed";
                booking.IsBookingConfirmed = true;

                // Mark all vendor bookings as approved
                foreach (var vendorBooking in booking.VendorBookings)
                {
                    if (vendorBooking.Status != "Approved")
                    {
                        vendorBooking.Status = "Approved";
                        vendorBooking.ApprovedAt = DateTime.UtcNow;
                        vendorBooking.RejectedAt = null;
                    }
                }

                var updatedBooking = await _bookingService.UpdateBookingAsync(booking);
                var bookingDto = _mapper.Map<BookingDto>(updatedBooking);

                return Success(bookingDto, "Booking force-approved by admin");
            }
            catch (Exception ex)
            {
                return Error<BookingDto>($"Failed to force approve booking: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Cancel booking (Admin can cancel any booking)
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("bookings/{id:int}")]
        public async Task<ActionResult<ApiResponse<string>>> CancelBookingAdmin(int id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);
                if (booking == null)
                {
                    return Error<string>($"Booking with ID {id} not found", 404);
                }

                var result = await _bookingService.CancelBookingAsync(id);
                if (!result)
                {
                    return Error<string>("Failed to cancel booking", 500);
                }

                return Success<string>("Booking cancelled successfully by admin");
            }
            catch (Exception ex)
            {
                return Error<string>($"Failed to cancel booking: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get booking statistics (dashboard metrics)
        /// </summary>
        /// <returns>Booking statistics</returns>
        [HttpGet("bookings/statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetBookingStatistics()
        {
            try
            {
                var allBookings = await _bookingService.GetAllBookingsAsync();

                var statistics = new
                {
                    totalBookings = allBookings.Count(),
                    pendingBookings = allBookings.Count(b => b.Status == "Pending"),
                    confirmedBookings = allBookings.Count(b => b.Status == "Confirmed"),
                    hallApprovedBookings = allBookings.Count(b => b.Status == "HallApproved"),
                    hallRejectedBookings = allBookings.Count(b => b.Status == "HallRejected"),
                    vendorRejectedBookings = allBookings.Count(b => b.Status == "VendorRejected"),
                    completedBookings = allBookings.Count(b => b.IsVisitCompleted),
                    totalRevenue = allBookings.Where(b => b.Status == "Confirmed").Sum(b => b.TotalAmount),
                    averageBookingValue = allBookings.Where(b => b.Status == "Confirmed").Any() 
                        ? allBookings.Where(b => b.Status == "Confirmed").Average(b => b.TotalAmount) 
                        : 0,
                    todayBookings = allBookings.Count(b => b.BookingDate.Date == DateTime.Today),
                    thisWeekBookings = allBookings.Count(b => b.BookingDate >= DateTime.Today.AddDays(-7)),
                    thisMonthBookings = allBookings.Count(b => b.BookingDate.Month == DateTime.Today.Month),
                    upcomingBookings = allBookings.Count(b => b.BookingDate > DateTime.Today && b.Status == "Confirmed")
                };

                return Success((object)statistics, "Booking statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to retrieve booking statistics: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get bookings by hall (Admin can view any hall's bookings)
        /// </summary>
        /// <param name="hallId">Hall ID</param>
        /// <returns>List of bookings for the hall</returns>
        [HttpGet("bookings/hall/{hallId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetBookingsByHallAdmin(int hallId)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByHallIdAsync(hallId);
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);
                return Success(bookingDtos, $"Bookings for hall {hallId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<BookingDto>>($"Failed to retrieve bookings: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get bookings by customer (Admin can view any customer's bookings)
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>List of customer's bookings</returns>
        [HttpGet("bookings/customer/{customerId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetBookingsByCustomerAdmin(string customerId)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByCustomerIdAsync(customerId);
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);
                return Success(bookingDtos, $"Bookings for customer {customerId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<BookingDto>>($"Failed to retrieve bookings: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get bookings by vendor (Admin can view any vendor's bookings)
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <returns>List of bookings involving the vendor</returns>
        [HttpGet("bookings/vendor/{vendorId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetBookingsByVendorAdmin(int vendorId)
        {
            try
            {
                var allBookings = await _bookingService.GetAllBookingsAsync();
                var vendorBookings = allBookings.Where(b => b.VendorBookings.Any(vb => vb.VendorId == vendorId));
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(vendorBookings);
                return Success(bookingDtos, $"Bookings for vendor {vendorId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<BookingDto>>($"Failed to retrieve bookings: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get bookings by date range (Admin reporting)
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of bookings in date range</returns>
        [HttpGet("bookings/date-range")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetBookingsByDateRangeAdmin(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var allBookings = await _bookingService.GetAllBookingsAsync();
                var dateRangeBookings = allBookings.Where(b => 
                    b.BookingDate >= startDate && b.BookingDate <= endDate);
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(dateRangeBookings);
                return Success(bookingDtos, $"Bookings from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd} retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<BookingDto>>($"Failed to retrieve bookings: {ex.Message}", 500);
            }
        }

        // Note: IBookingService interface needs to be injected in main AdminController constructor
        // Add: private readonly IBookingService _bookingService;
    }
}
