using AutoMapper;
using HallApp.Web.Controllers.Common;
using HallApp.Application.Common.Models;
using HallApp.Application.DTOs.Booking;
using HallApp.Application.DTOs.Booking.Registers;
using HallApp.Application.DTOs.Booking.Updaters;
using HallApp.Core.Entities.BookingEntities;
using BookingEntity = HallApp.Core.Entities.BookingEntities.Booking;
using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Booking
{
    /// <summary>
    /// Booking management controller
    /// Handles hall booking operations, updates, and status management
    /// </summary>
    [Route("api/v1/bookings")]
    public class BookingController : BaseApiController
    {
        private readonly IBookingService _bookingService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public BookingController(
            IBookingService bookingService, 
            IMapper mapper, 
            INotificationService notificationService)
        {
            _bookingService = bookingService;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Create new booking (Customer only)
        /// </summary>
        /// <param name="bookingDto">Booking registration data</param>
        /// <returns>Created booking details</returns>
        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<BookingDto>>> CreateBooking([FromBody] BookingRegisterDto bookingDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<BookingDto>($"Invalid booking data: {errors}", 400);
                }

                // Set the customer ID from the authenticated user
                bookingDto.CustomerId = UserId;

                var bookingEntity = _mapper.Map<HallApp.Core.Entities.BookingEntities.Booking>(bookingDto);
                var booking = await _bookingService.CreateBookingAsync(bookingEntity);

                if (booking == null)
                {
                    return Error<BookingDto>("Failed to create booking", 500);
                }

                // Send notification (async)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.CreateAndSendNotificationAsync(
                            booking.CustomerId,
                            "Booking Created",
                            $"Your booking for Hall ID {booking.HallId} has been successfully created. Booking ID: {booking.Id}"
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log notification error but don't fail the booking
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                });

                var resultDto = _mapper.Map<BookingDto>(booking);
                return Success(resultDto, "Booking created successfully");
            }
            catch (Exception ex)
            {
                return Error<BookingDto>($"Failed to create booking: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get booking by ID
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Booking details</returns>
        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<BookingDto>>> GetBookingById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Error<BookingDto>("Invalid booking ID", 400);
                }

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
        /// Get current user's bookings
        /// </summary>
        /// <returns>List of user's bookings</returns>
        [Authorize(Roles = "Customer")]
        [HttpGet("my-bookings")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetMyBookings()
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByCustomerIdAsync(UserId.ToString());
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);
                return Success(bookingDtos, "Your bookings retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<BookingDto>>($"Failed to retrieve your bookings: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get all bookings (Admin only)
        /// </summary>
        /// <returns>List of all bookings</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetAllBookings()
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
        /// Update booking
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="updateDto">Updated booking data</param>
        /// <returns>Updated booking details</returns>
        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<BookingDto>>> UpdateBooking(int id, [FromBody] BookingUpdateDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<BookingDto>($"Invalid update data: {errors}", 400);
                }

                // Get existing booking to check permissions
                var existingBooking = await _bookingService.GetBookingByIdAsync(id);
                if (existingBooking == null)
                {
                    return Error<BookingDto>($"Booking with ID {id} not found", 404);
                }

                updateDto.Id = id;

                BookingDto updatedBooking;

                if (IsAdmin)
                {
                    // Admin can update all fields
                    var entityToUpdate = _mapper.Map<HallApp.Core.Entities.BookingEntities.Booking>(updateDto);
                    var updatedEntity = await _bookingService.UpdateBookingAsync(entityToUpdate);
                    updatedBooking = _mapper.Map<BookingDto>(updatedEntity);
                }
                else if (existingBooking.CustomerId == UserId)
                {
                    // Customer can only update limited fields
                    var entityToUpdate = _mapper.Map<HallApp.Core.Entities.BookingEntities.Booking>(updateDto);
                    var updatedEntity = await _bookingService.UpdateCustomerBookingAsync(UserId.ToString(), entityToUpdate);
                    updatedBooking = _mapper.Map<BookingDto>(updatedEntity);
                }
                else
                {
                    return Error<BookingDto>("You can only update your own bookings", 403);
                }

                if (updatedBooking == null)
                {
                    return Error<BookingDto>("Failed to update booking", 500);
                }

                // Send notification for status changes
                if (updatedBooking != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationService.CreateAndSendNotificationAsync(
                                updatedBooking.CustomerId,
                                "Booking Updated",
                                $"Your booking for Hall ID {updatedBooking.HallId} has been updated. Status: {updatedBooking.Status}"
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Notification error: {ex.Message}");
                        }
                    });
                }

                return Success(updatedBooking, "Booking updated successfully");
            }
            catch (Exception ex)
            {
                return Error<BookingDto>($"Failed to update booking: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Cancel booking
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Success response</returns>
        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<string>>> CancelBooking(int id)
        {
            try
            {
                var existingBooking = await _bookingService.GetBookingByIdAsync(id);
                if (existingBooking == null)
                {
                    return Error<string>($"Booking with ID {id} not found", 404);
                }

                // Check permissions
                if (!IsAdmin && existingBooking.CustomerId != UserId)
                {
                    return Error<string>("You can only cancel your own bookings", 403);
                }

                var result = await _bookingService.CancelBookingAsync(id);
                if (!result)
                {
                    return Error<string>("Failed to cancel booking", 500);
                }

                // Send cancellation notification
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var booking = await _bookingService.GetBookingByIdAsync(id);
                        if (booking != null)
                        {
                            await _notificationService.CreateAndSendNotificationAsync(
                                booking.CustomerId,
                                "Booking Cancelled",
                                $"Your booking for Hall ID {booking.HallId} has been cancelled. Booking ID: {id}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                });

                return Success<string>("Booking cancelled successfully", "Booking cancelled successfully");
            }
            catch (Exception ex)
            {
                return Error<string>($"Failed to cancel booking: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get booking statistics (Admin only)
        /// </summary>
        /// <returns>Booking statistics</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<BookingStatisticsDto>>> GetBookingStatistics()
        {
            try
            {
                var stats = await _bookingService.GetBookingStatisticsAsync();
                return Success(stats, "Booking statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<BookingStatisticsDto>($"Failed to retrieve booking statistics: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get hall availability for date range
        /// </summary>
        /// <param name="hallId">Hall ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Hall availability information</returns>
        [AllowAnonymous]
        [HttpGet("availability")]
        public async Task<ActionResult<ApiResponse<HallAvailabilityDto>>> CheckHallAvailability(
            [FromQuery] int hallId, 
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate >= endDate)
                {
                    return Error<HallAvailabilityDto>("Start date must be before end date", 400);
                }

                if (startDate < DateTime.Today)
                {
                    return Error<HallAvailabilityDto>("Start date cannot be in the past", 400);
                }

                var isAvailable = await _bookingService.CheckHallAvailabilityAsync(hallId, startDate, endDate);
                var availability = new HallAvailabilityDto
                {
                    HallId = hallId,
                    Date = startDate,
                    IsAvailable = isAvailable
                };
                return Success(availability, "Hall availability checked successfully");
            }
            catch (Exception ex)
            {
                return Error<HallAvailabilityDto>($"Failed to check hall availability: {ex.Message}", 500);
            }
        }
    }
}
