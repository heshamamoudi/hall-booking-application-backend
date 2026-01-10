using AutoMapper;
using HallApp.Application.Common.Models;
using HallApp.Application.DTOs.Booking;
using HallApp.Application.DTOs.Booking.Registers;
using HallApp.Application.DTOs.Booking.Updaters;
using HallApp.Application.DTOs.Vendors;
using HallApp.Application.DTOs.Champer.Hall;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Application.Services;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BookingEntity = HallApp.Core.Entities.BookingEntities.Booking;

#nullable enable

namespace HallApp.Web.Controllers.Booking
{
    /// <summary>
    /// Booking management controller
    /// Handles hall booking operations, updates, and status management
    /// </summary>
    [Route("api/bookings")]
    public class BookingController : BaseApiController
    {
        private readonly IBookingService _bookingService;
        private readonly IMapper _mapper;
        private readonly IServiceItemService _serviceItemService;
        private readonly INotificationService _notificationService;
        private readonly IBookingFinancialService _financialService;
        private readonly IHallAvailabilityService _availabilityService;
        private readonly IPriceCalculationService _priceCalculationService;

        public BookingController(
            IBookingService bookingService,
            IMapper mapper,
            IServiceItemService serviceItemService,
            INotificationService notificationService,
            IBookingFinancialService financialService,
            IHallAvailabilityService availabilityService,
            IPriceCalculationService priceCalculationService)
        {
            _bookingService = bookingService;
            _mapper = mapper;
            _serviceItemService = serviceItemService;
            _notificationService = notificationService;
            _financialService = financialService;
            _availabilityService = availabilityService;
            _priceCalculationService = priceCalculationService;
        }

        /// <summary>
        /// Create new booking with vendor services (Customer only)
        /// </summary>
        /// <param name="bookingDto">Booking registration data with vendor services</param>
        /// <returns>Created booking details</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpPost("with-services")]
        public async Task<ActionResult<ApiResponse<BookingDto>>> CreateBookingWithServices([FromBody] BookingRequestDto bookingDto)
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

                // Validate booking time using availability service
                var (isValid, errorMessage) = await _availabilityService.ValidateBookingTimeAsync(
                    bookingDto.HallId,
                    bookingDto.EventDate,
                    bookingDto.StartTime,
                    bookingDto.EndTime);

                if (!isValid)
                {
                    return Error<BookingDto>(errorMessage, 400);
                }

                // Calculate comprehensive financial breakdown
                var eventStart = bookingDto.EventDate.Add(bookingDto.StartTime);
                var eventEnd = bookingDto.EventDate.Add(bookingDto.EndTime);
                
                var serviceRequests = bookingDto.SelectedServices?.Select(s => new BookingServiceRequest
                {
                    ServiceItemId = s.ServiceItemId,
                    VendorId = s.VendorId,
                    Quantity = s.Quantity,
                    SpecialInstructions = s.SpecialInstructions ?? string.Empty
                }).ToList() ?? new List<BookingServiceRequest>();

                var financialBreakdown = await _financialService.CalculateBookingFinancialsAsync(
                    bookingDto.HallId,
                    eventStart,
                    eventEnd,
                    serviceRequests,
                    bookingDto.DiscountCode ?? string.Empty,
                    "Riyadh" // TODO: Get customer's region
                );

                // Create booking entity from DTO with financial data
                var bookingEntity = new BookingEntity
                {
                    HallId = bookingDto.HallId,
                    CustomerId = bookingDto.CustomerId,
                    BookingDate = bookingDto.EventDate,
                    EventDate = bookingDto.EventDate,
                    StartTime = bookingDto.StartTime,
                    EndTime = bookingDto.EndTime,
                    EventType = bookingDto.EventType ?? "Event",
                    GuestCount = bookingDto.ExpectedGuestCount,
                    GenderPreference = bookingDto.GenderPreference,
                    Status = "Pending", // Awaiting hall approval
                    Comments = bookingDto.SpecialRequests,
                    
                    // Financial breakdown from service
                    HallCost = financialBreakdown.HallCost,
                    VendorServicesCost = financialBreakdown.VendorServicesCost,
                    Subtotal = financialBreakdown.Subtotal,
                    DiscountAmount = financialBreakdown.DiscountAmount,
                    TaxAmount = financialBreakdown.TaxAmount,
                    TaxRate = financialBreakdown.TaxRate,
                    TotalAmount = financialBreakdown.TotalAmount,
                    Currency = financialBreakdown.Currency,
                    Coupon = bookingDto.DiscountCode ?? string.Empty,
                    
                    IsBookingConfirmed = false,
                    IsVisitCompleted = false,
                    Created = DateTime.UtcNow
                };

                // Create VendorBookings before saving the booking
                if (bookingDto.SelectedServices?.Any() == true)
                {
                    var servicesByVendor = bookingDto.SelectedServices.GroupBy(s => s.VendorId);
                    
                    foreach (var vendorGroup in servicesByVendor)
                    {
                        var vendorId = vendorGroup.Key;
                        var services = vendorGroup.ToList();
                        
                        // Get vendor financial data from breakdown
                        var vendorBreakdown = financialBreakdown.VendorBreakdown
                            .FirstOrDefault(vb => vb.VendorId == vendorId);
                        
                        var vendorTotalAmount = vendorBreakdown?.TotalAmount ?? 0;
                        var vendorBookingServices = new List<VendorBookingService>();
                        
                        foreach (var service in services)
                        {
                            // Get service financial details from breakdown
                            var serviceDetail = vendorBreakdown?.Services
                                .FirstOrDefault(s => s.ServiceItemId == service.ServiceItemId);
                            
                            var servicePrice = serviceDetail?.UnitPrice ?? 0;
                            var serviceTotalPrice = serviceDetail?.TotalPrice ?? 0;
                            
                            vendorBookingServices.Add(new VendorBookingService
                            {
                                ServiceItemId = service.ServiceItemId,
                                Quantity = service.Quantity,
                                SpecialInstructions = service.SpecialInstructions,
                                UnitPrice = servicePrice,
                                TotalPrice = serviceTotalPrice,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                        
                        // Create single VendorBooking for this vendor
                        var vendorBooking = new VendorBooking
                        {
                            VendorId = vendorId,
                            StartTime = bookingDto.EventDate.Add(bookingDto.StartTime),
                            EndTime = bookingDto.EventDate.Add(bookingDto.EndTime),
                            Status = "Pending",
                            TotalAmount = vendorTotalAmount,
                            ServiceDate = bookingDto.EventDate,
                            Services = vendorBookingServices,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        
                        bookingEntity.VendorBookings.Add(vendorBooking);
                    }
                }

                var booking = await _bookingService.CreateBookingAsync(bookingEntity);
                if (booking == null)
                {
                    return Error<BookingDto>("Failed to create booking", 500);
                }

                // Send notification to customer
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            booking.CustomerId,
                            "Booking Created - Awaiting Approval",
                            $"Your booking for {bookingDto.EventDate:yyyy-MM-dd} has been created and is awaiting hall approval. Booking ID: {booking.Id}",
                            "Booking"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                });

                var bookingResponseDto = _mapper.Map<BookingDto>(booking);
                return Success(bookingResponseDto, "Booking created successfully - awaiting hall approval");
            }
            catch (Exception ex)
            {
                return Error<BookingDto>($"Failed to create booking: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get booking approval status (Customer only)
        /// </summary>
        /// <param name="bookingId">Booking ID</param>
        /// <returns>Booking approval status</returns>
        [Authorize(Roles = "Admin,Customer,HallManager,VendorManager")]
        [HttpGet("{bookingId:int}/approval-status")]
        public async Task<ActionResult<ApiResponse<object>>> GetBookingApprovalStatus(int bookingId)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(bookingId);
                if (booking == null || booking.CustomerId != UserId)
                {
                    return Error<object>("Booking not found", 404);
                }

                // Create approval status response with grouped vendor bookings
                var approvals = new List<object>();
                
                // Hall approval
                approvals.Add(new {
                    id = 0,
                    bookingId = booking.Id,
                    type = "hall",
                    hallId = booking.HallId,
                    vendorId = (int?)null,
                    vendorName = (string?)null,
                    status = booking.Status == "Pending" ? "pending" : 
                            booking.Status.StartsWith("Hall") && booking.Status != "HallRejected" ? "approved" : 
                            booking.Status == "HallRejected" ? "rejected" : "pending",
                    createdAt = booking.CreatedAt,
                    approvedAt = booking.Status.StartsWith("Hall") && booking.Status != "HallRejected" ? booking.UpdatedAt : (DateTime?)null,
                    servicesCount = 0
                });
                
                // Vendor approvals (now grouped by vendor)
                foreach (var vendorBooking in booking.VendorBookings)
                {
                    approvals.Add(new {
                        id = vendorBooking.Id,
                        bookingId = booking.Id,
                        type = "vendor",
                        hallId = (int?)null,
                        vendorId = vendorBooking.VendorId,
                        vendorName = vendorBooking.Vendor?.Name ?? $"Vendor {vendorBooking.VendorId}",
                        status = vendorBooking.Status.ToLower(),
                        createdAt = vendorBooking.CreatedAt,
                        approvedAt = vendorBooking.ApprovedAt,
                        rejectedAt = vendorBooking.RejectedAt,
                        servicesCount = vendorBooking.Services.Count,
                        totalAmount = vendorBooking.TotalAmount
                    });
                }
                
                return Success((object)new {
                    bookingId = booking.Id,
                    status = booking.Status,
                    approvals = approvals
                });
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to get approval status: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get alternative halls for rejected booking (Customer only)
        /// </summary>
        /// <param name="bookingId">Booking ID</param>
        /// <param name="eventDate">Event date</param>
        /// <returns>Available alternative halls</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpGet("{bookingId:int}/alternative-halls")]
        public async Task<ActionResult<ApiResponse<object>>> GetAlternativeHalls(int bookingId, [FromQuery] string eventDate)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(bookingId);
                if (booking == null || booking.CustomerId != UserId)
                {
                    return Error<object>("Booking not found", 404);
                }

                if (!DateTime.TryParse(eventDate, out var parsedDate))
                {
                    return Error<object>("Invalid event date format", 400);
                }

                // TODO: Implement alternative halls service when hall service is available
                var alternativeHalls = new List<object>();
                
                return Success((object)new {
                    alternativeHalls = alternativeHalls,
                    message = "No alternative halls available at the moment"
                });
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to get alternative halls: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Approve booking by hall manager (HallManager role only)
        /// </summary>
        /// <param name="bookingId">Booking ID</param>
        /// <param name="approved">Approval status</param>
        /// <returns>Updated booking details</returns>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpPost("{bookingId:int}/hall-approval")]
        public async Task<ActionResult<ApiResponse<BookingDto>>> ApproveBookingByHall(int bookingId, [FromBody] bool approved)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return Error<BookingDto>("Invalid booking ID", 400);
                }

                var booking = await _bookingService.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return Error<BookingDto>("Booking not found", 404);
                }

                if (approved)
                {
                    // Hall approves booking
                    booking.Status = "HallApproved";
                    booking.IsBookingConfirmed = false; // Still need vendor approvals
                    
                    // Notify customer
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationService.CreateNotificationAsync(
                                booking.CustomerId,
                                "Hall Approved Your Booking",
                                $"Great news! The hall has approved your booking for {booking.BookingDate:yyyy-MM-dd}. Vendors will now process their approvals.",
                                "Booking"
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Notification error: {ex.Message}");
                        }
                    });
                }
                else
                {
                    // Hall rejects booking - provide alternatives
                    booking.Status = "HallRejected";
                    booking.IsBookingConfirmed = false;
                    
                    // Notify customer with alternatives option
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationService.CreateNotificationAsync(
                                booking.CustomerId,
                                "Hall Rejected - Alternatives Available",
                                $"The hall has rejected your booking for {booking.BookingDate:yyyy-MM-dd}. We found alternative halls available for the same date. Please check your booking options.",
                                "Booking"
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Notification error: {ex.Message}");
                        }
                    });
                }

                var updatedBooking = await _bookingService.UpdateBookingAsync(booking);
                var bookingDto = _mapper.Map<BookingDto>(updatedBooking);

                string message = approved ? "Booking approved by hall" : "Booking rejected by hall";
                return Success(bookingDto, message);
            }
            catch (Exception ex)
            {
                return Error<BookingDto>($"Failed to process hall approval: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Approve booking by vendor (VendorManager role only)
        /// </summary>
        /// <param name="bookingId">Booking ID</param>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="approved">Approval status</param>
        /// <returns>Updated booking details</returns>
        [Authorize(Roles = "Admin,VendorManager")]
        [HttpPost("{bookingId:int}/vendor-approval/{vendorId:int}")]
        public async Task<ActionResult<ApiResponse<BookingDto>>> ApproveBookingByVendor(int bookingId, int vendorId, [FromBody] bool approved)
        {
            try
            {
                if (bookingId <= 0 || vendorId <= 0)
                {
                    return Error<BookingDto>("Invalid booking or vendor ID", 400);
                }

                var booking = await _bookingService.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return Error<BookingDto>("Booking not found", 404);
                }

                if (booking.Status != "HallApproved")
                {
                    return Error<BookingDto>("Booking must be approved by hall first", 400);
                }

                // Find the vendor booking (now there's only 1 per vendor)
                var vendorBooking = booking.VendorBookings.FirstOrDefault(vb => vb.VendorId == vendorId);
                if (vendorBooking == null)
                {
                    return Error<BookingDto>("Vendor not associated with this booking", 400);
                }

                // Update vendor booking status (affects all their services)
                if (approved)
                {
                    vendorBooking.Status = "Approved";
                    vendorBooking.ApprovedAt = DateTime.UtcNow;
                    vendorBooking.RejectedAt = null;
                }
                else
                {
                    vendorBooking.Status = "Rejected";
                    vendorBooking.RejectedAt = DateTime.UtcNow;
                    vendorBooking.ApprovedAt = null;
                }

                // Check overall vendor approval status
                var allVendorBookings = booking.VendorBookings.ToList();
                var pendingVendors = allVendorBookings.Where(vb => vb.Status == "Pending").ToList();
                var approvedVendors = allVendorBookings.Where(vb => vb.Status == "Approved").ToList();
                var rejectedVendors = allVendorBookings.Where(vb => vb.Status == "Rejected").ToList();

                string notificationTitle;
                string notificationMessage;

                if (pendingVendors.Any())
                {
                    // Still waiting for other vendors
                    booking.Status = "VendorApprovalInProgress";
                    booking.IsBookingConfirmed = false;
                    
                    var totalVendors = allVendorBookings.Count;
                    var respondedVendors = allVendorBookings.Count(vb => vb.Status != "Pending");
                    
                    notificationTitle = "Vendor Response Received";
                    notificationMessage = $"A vendor has {(approved ? "approved" : "rejected")} your booking. " +
                                        $"Progress: {respondedVendors}/{totalVendors} vendors responded.";
                }
                else if (rejectedVendors.Any())
                {
                    // Some or all vendors rejected
                    booking.Status = "VendorRejected";
                    booking.IsBookingConfirmed = false;
                    
                    var rejectedVendorNames = rejectedVendors.Select(vb => vb.Vendor?.Name ?? "Unknown").ToList();
                    
                    notificationTitle = "Vendor Rejected - Alternatives Available";
                    notificationMessage = $"Some vendors have rejected your booking for {booking.BookingDate:yyyy-MM-dd}. " +
                                        $"Rejected: {string.Join(", ", rejectedVendorNames)}. Please select alternatives.";
                }
                else
                {
                    // All vendors approved
                    booking.Status = "Confirmed";
                    booking.IsBookingConfirmed = true;
                    
                    notificationTitle = "Booking Fully Confirmed!";
                    notificationMessage = $"Excellent! All vendors have approved your booking for {booking.BookingDate:yyyy-MM-dd}. " +
                                        "Your event is fully confirmed and ready for payment.";
                }

                // Send notification
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            booking.CustomerId,
                            notificationTitle,
                            notificationMessage,
                            "Booking"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                });

                var updatedBooking = await _bookingService.UpdateBookingAsync(booking);
                var bookingDto = _mapper.Map<BookingDto>(updatedBooking);
                
                string message = approved ? "Booking approved by vendor" : "Booking rejected by vendor";
                return Success(bookingDto, message);
            }
            catch (Exception ex)
            {
                return Error<BookingDto>($"Failed to process vendor approval: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Replace hall in rejected booking (Customer only)
        /// </summary>
        /// <param name="bookingId">Original booking ID</param>
        /// <param name="newHallId">New hall ID to replace rejected hall</param>
        /// <returns>Updated booking details</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpPost("{bookingId:int}/replace-hall/{newHallId:int}")]
        public async Task<ActionResult<ApiResponse<BookingDto>>> ReplaceHallInBooking(int bookingId, int newHallId)
        {
            try
            {
                if (bookingId <= 0 || newHallId <= 0)
                {
                    return Error<BookingDto>("Invalid booking or hall ID", 400);
                }

                var booking = await _bookingService.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return Error<BookingDto>("Booking not found", 404);
                }

                // Verify the booking belongs to the customer
                if (booking.CustomerId != UserId)
                {
                    return Error<BookingDto>("You can only modify your own bookings", 403);
                }

                // Only allow replacement if hall was rejected
                if (booking.Status != "HallRejected")
                {
                    return Error<BookingDto>("Hall can only be replaced if it was rejected", 400);
                }

                // Update booking with new hall and reset to pending
                booking.HallId = newHallId;
                booking.Status = "Pending";
                booking.IsBookingConfirmed = false;

                // Recalculate cost for new hall
                var eventStart = booking.BookingDate;
                var eventEnd = booking.BookingDate.AddHours(4); // Default 4-hour event
                var newCost = await _bookingService.CalculateBookingCostAsync(newHallId, eventStart, eventEnd);
                booking.TotalAmount = newCost;

                var updatedBooking = await _bookingService.UpdateBookingAsync(booking);
                
                // Notify customer
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            booking.CustomerId,
                            "Hall Replacement Successful",
                            $"Your booking has been updated with a new hall. Your booking is now pending hall approval again. Booking ID: {booking.Id}",
                            "Booking"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                });

                var bookingDto = _mapper.Map<BookingDto>(updatedBooking);
                return Success(bookingDto, "Hall replaced successfully - booking is now pending approval");
            }
            catch (Exception ex)
            {
                return Error<BookingDto>($"Failed to replace hall: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Replace vendor service in rejected booking (Customer only)
        /// </summary>
        /// <param name="bookingId">Original booking ID</param>
        /// <param name="replacementDto">Vendor replacement data containing rejected and new vendor info</param>
        /// <returns>Updated booking details</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpPost("{bookingId:int}/replace-vendor")]
        public async Task<ActionResult<ApiResponse<BookingDto>>> ReplaceVendorInBooking(
            int bookingId, 
            [FromBody] VendorReplacementDto replacementDto)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return Error<BookingDto>("Invalid booking ID", 400);
                }

                var booking = await _bookingService.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return Error<BookingDto>("Booking not found", 404);
                }

                // Verify the booking belongs to the customer
                if (booking.CustomerId != UserId)
                {
                    return Error<BookingDto>("You can only modify your own bookings", 403);
                }

                // Only allow replacement if vendor was rejected
                if (booking.Status != "VendorRejected")
                {
                    return Error<BookingDto>("Vendor can only be replaced if one was rejected", 400);
                }

                // TODO: Update booking package with new vendor services
                // This would require extending the booking model to track individual vendor services
                
                // Reset booking status to hall approved (since hall already approved)
                booking.Status = "HallApproved";
                booking.IsBookingConfirmed = false;

                var updatedBooking = await _bookingService.UpdateBookingAsync(booking);
                
                // Notify customer
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            booking.CustomerId,
                            "Vendor Replacement Successful",
                            $"Your booking has been updated with a new vendor. The vendor approval process will begin again. Booking ID: {booking.Id}",
                            "Booking"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                });

                var bookingDto = _mapper.Map<BookingDto>(updatedBooking);
                return Success(bookingDto, "Vendor replaced successfully - awaiting new vendor approval");
            }
            catch (Exception ex)
            {
                return Error<BookingDto>($"Failed to replace vendor: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Create new booking (Customer only)
        /// </summary>
        /// <param name="bookingDto">Booking registration data</param>
        /// <returns>Created booking details</returns>
        [Authorize(Roles = "Admin,Customer")]
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
                        await _notificationService.CreateNotificationAsync(
                            booking.CustomerId,
                            "Booking Created",
                            $"Your booking for Hall ID {booking.HallId} has been successfully created. Booking ID: {booking.Id}",
                            "Booking"
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
            if (id <= 0)
            {
                return Error<BookingDto>("Invalid booking ID", 400);
            }

            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);
                if (booking == null)
                {
                    return Error<BookingDto>($"Booking with ID {id} not found", 404);
                }

                var bookingDto = _mapper.Map<BookingDto>(booking);
                return Success(bookingDto);
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
        [Authorize(Roles = "Admin,Customer")]
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
        /// Get bookings by hall (Hall Manager and Admin)
        /// </summary>
        /// <param name="hallId">Hall ID</param>
        /// <returns>List of bookings for the specified hall</returns>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpGet("hall/{hallId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetBookingsByHall(int hallId)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByHallIdAsync(hallId);
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);
                return Success(bookingDtos, $"Bookings for hall {hallId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<BookingDto>>($"Failed to retrieve bookings for hall: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get bookings by vendor (Vendor Manager and Admin)
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <returns>List of bookings for the specified vendor</returns>
        [Authorize(Roles = "Admin,VendorManager")]
        [HttpGet("vendor/{vendorId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetBookingsByVendor(int vendorId)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByVendorIdAsync(vendorId);
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);
                return Success(bookingDtos, $"Bookings for vendor {vendorId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<BookingDto>>($"Failed to retrieve bookings for vendor: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get bookings by customer (Customer and Admin)
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>List of bookings for the specified customer</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetBookingsByCustomer(string customerId)
        {
            try
            {
                // Parse customerId to int for comparison
                if (!int.TryParse(customerId, out int customerIdInt))
                {
                    return Error<IEnumerable<BookingDto>>("Invalid customer ID format", 400);
                }

                // Customers can only access their own bookings
                if (!IsAdmin && customerIdInt != UserId)
                {
                    return Error<IEnumerable<BookingDto>>("Access denied", 403);
                }

                var bookings = await _bookingService.GetCustomerBookingsAsync(customerId);
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);
                return Success(bookingDtos, $"Bookings for customer retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<BookingDto>>($"Failed to retrieve bookings for customer: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update booking
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="updateDto">Updated booking data</param>
        /// <returns>Updated booking details</returns>
        [Authorize(Roles = "Admin,Customer,HallManager")]
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
                            await _notificationService.CreateNotificationAsync(
                                updatedBooking.CustomerId,
                                "Booking Updated",
                                $"Your booking for Hall ID {updatedBooking.HallId} has been updated. Status: {updatedBooking.Status}",
                                "Booking"
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Notification error: {ex.Message}");
                        }
                    });
                }

                if (updatedBooking == null)
                    return Error<BookingDto>("Failed to update booking", 500);
                    
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
        [Authorize(Roles = "Admin,Customer")]
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
                            await _notificationService.CreateNotificationAsync(
                                booking.CustomerId,
                                "Booking Cancelled",
                                $"Your booking for Hall ID {booking.HallId} has been cancelled. Booking ID: {id}",
                                "Booking"
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

        /// <summary>
        /// Get available time slots for a specific hall and date
        /// </summary>
        /// <param name="hallId">Hall ID</param>
        /// <param name="date">Date to check</param>
        /// <returns>List of available time slots</returns>
        [AllowAnonymous]
        [HttpGet("availability/timeslots")]
        public async Task<ActionResult<ApiResponse<object>>> GetAvailableTimeSlots(
            [FromQuery] int hallId,
            [FromQuery] DateTime date)
        {
            try
            {
                if (date.Date < DateTime.UtcNow.Date)
                {
                    return Error<object>("Date cannot be in the past", 400);
                }

                var timeSlots = await _availabilityService.GetAvailableTimeSlotsAsync(hallId, date);

                var response = new
                {
                    hallId = hallId,
                    date = date.Date,
                    timeSlots = timeSlots.Select(ts => new
                    {
                        startTime = ts.StartTimeFormatted,
                        endTime = ts.EndTimeFormatted,
                        isAvailable = ts.IsAvailable,
                        durationHours = ts.DurationHours
                    })
                };

                return Success((object)response, "Time slots retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to get time slots: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get available dates for a hall within a date range
        /// </summary>
        /// <param name="hallId">Hall ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of available dates</returns>
        [AllowAnonymous]
        [HttpGet("availability/dates")]
        public async Task<ActionResult<ApiResponse<object>>> GetAvailableDates(
            [FromQuery] int hallId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate.Date < DateTime.UtcNow.Date)
                {
                    return Error<object>("Start date cannot be in the past", 400);
                }

                if (startDate >= endDate)
                {
                    return Error<object>("Start date must be before end date", 400);
                }

                if ((endDate - startDate).Days > 90)
                {
                    return Error<object>("Date range cannot exceed 90 days", 400);
                }

                var availableDates = await _availabilityService.GetAvailableDatesAsync(hallId, startDate, endDate);

                var response = new
                {
                    hallId = hallId,
                    startDate = startDate.Date,
                    endDate = endDate.Date,
                    availableDates = availableDates.Select(d => d.Date.ToString("yyyy-MM-dd"))
                };

                return Success((object)response, "Available dates retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to get available dates: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Calculate comprehensive booking pricing (Admin/Customer)
        /// Uses server-side price calculation service with dynamic pricing rules
        /// </summary>
        /// <param name="pricingRequest">Pricing calculation request with all parameters</param>
        /// <returns>Detailed pricing breakdown</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpPost("calculate-pricing")]
        public async Task<ActionResult<ApiResponse<object>>> CalculateBookingPricing([FromBody] BookingPricingRequestDto pricingRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<object>($"Invalid pricing request: {errors}", 400);
                }

                // Extract service item IDs from selected services
                List<int>? serviceItemIds = pricingRequest.SelectedServices?
                    .Select(s => s.ServiceItemId)
                    .ToList();

                // Use PriceCalculationService for comprehensive pricing
                var breakdown = await _priceCalculationService.CalculateBookingPriceAsync(
                    pricingRequest.HallId,
                    pricingRequest.EventDate,
                    pricingRequest.StartTime,
                    pricingRequest.EndTime,
                    serviceItemIds,
                    pricingRequest.DiscountCode
                );

                // Build detailed response
                var isWeekend = pricingRequest.EventDate.DayOfWeek == DayOfWeek.Friday ||
                               pricingRequest.EventDate.DayOfWeek == DayOfWeek.Saturday;
                var eventTime = pricingRequest.StartTime;
                var isEvening = eventTime >= new TimeSpan(18, 0, 0);
                var duration = (pricingRequest.EndTime - pricingRequest.StartTime).TotalHours;

                var pricingBreakdown = new
                {
                    hallCost = new
                    {
                        amount = breakdown.HallCost,
                        durationHours = duration,
                        isWeekend = isWeekend,
                        isEvening = isEvening,
                        dayType = isWeekend ? "Weekend" : "Weekday",
                        timeType = isEvening ? "Evening" : "Daytime"
                    },
                    vendorServices = new
                    {
                        totalAmount = breakdown.VendorServicesCost,
                        itemCount = serviceItemIds?.Count ?? 0
                    },
                    subtotal = breakdown.Subtotal,
                    discount = new
                    {
                        code = breakdown.DiscountCode,
                        amount = breakdown.DiscountAmount,
                        applied = breakdown.DiscountAmount > 0
                    },
                    tax = new
                    {
                        rate = breakdown.TaxRate,
                        amount = breakdown.TaxAmount,
                        percentage = $"{breakdown.TaxRate * 100}%"
                    },
                    totalAmount = breakdown.TotalAmount,
                    currency = breakdown.Currency,
                    calculatedAt = DateTime.UtcNow,
                    pricingFactors = new
                    {
                        weekendPremium = isWeekend ? "25%" : "None",
                        eveningPremium = isEvening ? "10%" : "None",
                        longBookingDiscount = duration > 8 ? "5%" : "None"
                    }
                };

                Console.WriteLine($"🧮 Pricing Calculation - Hall: {breakdown.HallCost} SAR, Services: {breakdown.VendorServicesCost} SAR, Discount: {breakdown.DiscountAmount} SAR, Tax: {breakdown.TaxAmount} SAR, Total: {breakdown.TotalAmount} SAR");

                return Success((object)pricingBreakdown, "Pricing calculated successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to calculate pricing: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Validate discount code (Admin/Customer)
        /// </summary>
        /// <param name="discountCode">Discount code to validate</param>
        /// <returns>Discount validation result</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpGet("validate-discount/{discountCode}")]
        public async Task<ActionResult<ApiResponse<object>>> ValidateDiscountCode(string discountCode)
        {
            try
            {
                var (isValid, message, discountPercentage) = await _priceCalculationService.ValidateDiscountCodeAsync(discountCode);

                var response = new
                {
                    code = discountCode,
                    isValid = isValid,
                    message = message,
                    discountPercentage = discountPercentage,
                    discountDisplay = isValid ? $"{discountPercentage * 100}% off" : null
                };

                return Success((object)response, message);
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to validate discount code: {ex.Message}", 500);
            }
        }

    }
}
