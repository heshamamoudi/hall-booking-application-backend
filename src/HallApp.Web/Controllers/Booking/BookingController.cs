using AutoMapper;
using HallApp.Core.Exceptions;
using HallApp.Application.DTOs.Booking;
using HallApp.Application.DTOs.Booking.Registers;
using HallApp.Application.DTOs.Booking.Updaters;
using HallApp.Application.DTOs.Vendors;
using HallApp.Application.DTOs.Halls.Hall;
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
        private readonly ICustomerService _customerService;
        private readonly IHallService _hallService;
        private readonly IHallManagerService _hallManagerService;
        private readonly IVendorManagerService _vendorManagerService;
        private readonly IOrganizationService _organizationService;
        private readonly ILogger<BookingController> _logger;

        public BookingController(
            IBookingService bookingService,
            IMapper mapper,
            IServiceItemService serviceItemService,
            INotificationService notificationService,
            IBookingFinancialService financialService,
            IHallAvailabilityService availabilityService,
            IPriceCalculationService priceCalculationService,
            ICustomerService customerService,
            IHallService hallService,
            IHallManagerService hallManagerService,
            IVendorManagerService vendorManagerService,
            IOrganizationService organizationService,
            ILogger<BookingController> logger)
        {
            _bookingService = bookingService;
            _mapper = mapper;
            _serviceItemService = serviceItemService;
            _notificationService = notificationService;
            _financialService = financialService;
            _availabilityService = availabilityService;
            _priceCalculationService = priceCalculationService;
            _customerService = customerService;
            _hallService = hallService;
            _hallManagerService = hallManagerService;
            _vendorManagerService = vendorManagerService;
            _organizationService = organizationService;
            _logger = logger;
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

                // Resolve Customer entity ID from AppUser ID (they are different!)
                var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                if (customer == null)
                {
                    return Error<BookingDto>("Customer profile not found. Please complete your profile first.", 404);
                }
                bookingDto.CustomerId = customer.Id;

                // Ensure EventDate has UTC kind for PostgreSQL timestamptz columns
                bookingDto.EventDate = DateTime.SpecifyKind(bookingDto.EventDate, DateTimeKind.Utc);

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
                    IsVisitCompleted = false
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

                // Use transactional booking creation with pessimistic locking to prevent race conditions
                var (success, booking, bookingError) = await _bookingService.CreateBookingWithLockingAsync(bookingEntity);
                if (!success || booking == null)
                {
                    return Error<BookingDto>(bookingError, 409); // 409 Conflict for double booking
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
                _logger.LogError(ex, "Error creating booking with services");
                return Error<BookingDto>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Add vendor services to an existing booking (Customer only)
        /// </summary>
        /// <param name="bookingId">Booking ID</param>
        /// <param name="services">List of vendor services to add</param>
        /// <returns>Updated booking details</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpPost("{bookingId:int}/add-vendor-services")]
        public async Task<ActionResult<ApiResponse<BookingDto>>> AddVendorServicesToBooking(
            int bookingId,
            [FromBody] List<VendorServiceSelectionDto> services)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return Error<BookingDto>("Invalid booking ID", 400);
                }

                if (services == null || !services.Any())
                {
                    return Error<BookingDto>("At least one service must be selected", 400);
                }

                // Get existing booking
                var booking = await _bookingService.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return Error<BookingDto>("Booking not found", 404);
                }

                // Verify ownership (resolve Customer entity ID from AppUser ID)
                var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                if (customer == null || (!IsAdmin && booking.CustomerId != customer.Id))
                {
                    return Error<BookingDto>("You can only modify your own bookings", 403);
                }

                // Only allow adding services when booking is in eligible status
                var eligibleStatuses = new[] { "Pending", "HallApproved" };
                if (!eligibleStatuses.Contains(booking.Status))
                {
                    return Error<BookingDto>(
                        $"Cannot add services when booking status is '{booking.Status}'. Services can only be added when status is Pending or HallApproved.",
                        400);
                }

                // Calculate financials for ALL services (existing + new)
                var allServiceRequests = new List<BookingServiceRequest>();

                // Include existing vendor booking services
                foreach (var existingVb in booking.VendorBookings)
                {
                    foreach (var existingSvc in existingVb.Services)
                    {
                        allServiceRequests.Add(new BookingServiceRequest
                        {
                            ServiceItemId = existingSvc.ServiceItemId,
                            VendorId = existingVb.VendorId,
                            Quantity = existingSvc.Quantity,
                            SpecialInstructions = existingSvc.SpecialInstructions ?? string.Empty
                        });
                    }
                }

                // Add new services
                foreach (var svc in services)
                {
                    allServiceRequests.Add(new BookingServiceRequest
                    {
                        ServiceItemId = svc.ServiceItemId,
                        VendorId = svc.VendorId,
                        Quantity = svc.Quantity,
                        SpecialInstructions = svc.SpecialInstructions ?? string.Empty
                    });
                }

                // Recalculate full financial breakdown
                var eventStart = DateTime.SpecifyKind(booking.EventDate.Add(booking.StartTime), DateTimeKind.Utc);
                var eventEnd = DateTime.SpecifyKind(booking.EventDate.Add(booking.EndTime), DateTimeKind.Utc);

                var financialBreakdown = await _financialService.CalculateBookingFinancialsAsync(
                    booking.HallId,
                    eventStart,
                    eventEnd,
                    allServiceRequests,
                    booking.Coupon ?? string.Empty,
                    "Riyadh"
                );

                // Create VendorBooking records for NEW services only
                var newServicesByVendor = services.GroupBy(s => s.VendorId);

                foreach (var vendorGroup in newServicesByVendor)
                {
                    var vendorId = vendorGroup.Key;
                    var vendorServices = vendorGroup.ToList();

                    var vendorBreakdown = financialBreakdown.VendorBreakdown
                        .FirstOrDefault(vb => vb.VendorId == vendorId);

                    var vendorTotalAmount = vendorBreakdown?.TotalAmount ?? 0;
                    var vendorBookingServices = new List<VendorBookingService>();

                    foreach (var svc in vendorServices)
                    {
                        var serviceDetail = vendorBreakdown?.Services
                            .FirstOrDefault(s => s.ServiceItemId == svc.ServiceItemId);

                        vendorBookingServices.Add(new VendorBookingService
                        {
                            ServiceItemId = svc.ServiceItemId,
                            Quantity = svc.Quantity,
                            SpecialInstructions = svc.SpecialInstructions,
                            UnitPrice = serviceDetail?.UnitPrice ?? 0,
                            TotalPrice = serviceDetail?.TotalPrice ?? 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }

                    // Check if vendor already has a VendorBooking for this booking
                    var existingVendorBooking = booking.VendorBookings
                        .FirstOrDefault(vb => vb.VendorId == vendorId);

                    if (existingVendorBooking != null)
                    {
                        // Add services to existing VendorBooking
                        foreach (var vbs in vendorBookingServices)
                        {
                            existingVendorBooking.Services.Add(vbs);
                        }
                        existingVendorBooking.TotalAmount += vendorTotalAmount;
                        existingVendorBooking.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Create new VendorBooking
                        var vendorBooking = new VendorBooking
                        {
                            VendorId = vendorId,
                            StartTime = eventStart,
                            EndTime = eventEnd,
                            Status = "Pending",
                            TotalAmount = vendorTotalAmount,
                            ServiceDate = DateTime.SpecifyKind(booking.EventDate, DateTimeKind.Utc),
                            Services = vendorBookingServices,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        booking.VendorBookings.Add(vendorBooking);
                    }
                }

                // Update financial totals on booking
                booking.VendorServicesCost = financialBreakdown.VendorServicesCost;
                booking.Subtotal = financialBreakdown.Subtotal;
                booking.DiscountAmount = financialBreakdown.DiscountAmount;
                booking.TaxAmount = financialBreakdown.TaxAmount;
                booking.TaxRate = financialBreakdown.TaxRate;
                booking.TotalAmount = financialBreakdown.TotalAmount;

                var updatedBooking = await _bookingService.UpdateBookingAsync(booking);

                // Notify customer
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            booking.CustomerId,
                            "Vendor Services Added",
                            $"Vendor services have been added to your booking. Updated total: {financialBreakdown.TotalAmount} {financialBreakdown.Currency}. Booking ID: {booking.Id}",
                            "Booking"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                });

                var bookingResponseDto = _mapper.Map<BookingDto>(updatedBooking);
                return Success(bookingResponseDto, "Vendor services added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding vendor services to booking");
                return Error<BookingDto>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Get booking approval status (Customer only)
        /// </summary>
        /// <param name="bookingId">Booking ID</param>
        /// <returns>Booking approval status</returns>
        [Authorize(Roles = "Admin,Customer,HallOrganizationManager,HallManager,VendorOrganizationManager,VendorManager")]
        [HttpGet("{bookingId:int}/approval-status")]
        public async Task<ActionResult<ApiResponse<object>>> GetBookingApprovalStatus(int bookingId)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(bookingId);
                // Resolve Customer entity ID for ownership check
                var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                if (booking == null || (customer != null && booking.CustomerId != customer.Id && !IsAdmin))
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
                _logger.LogError(ex, "Error getting approval status for booking {BookingId}", bookingId);
                return Error<object>("An error occurred processing your request. Please try again.", 500);
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
                var altCustomer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                if (booking == null || (altCustomer != null && booking.CustomerId != altCustomer.Id && !IsAdmin))
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
                _logger.LogError(ex, "Error getting alternative halls for booking {BookingId}", bookingId);
                return Error<object>("An error occurred processing your request. Please try again.", 500);
            }
        }

        // NOTE: Hall approval and vendor approval endpoints are in BookingApprovalController
        // to avoid route conflicts. Use POST api/bookings/{bookingId}/hall-approval
        // and POST api/bookings/{bookingId}/vendor-bookings/{vendorBookingId}/approval

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

                // Verify the booking belongs to the customer (resolve Customer entity ID)
                var replaceCustomer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                if (replaceCustomer == null || booking.CustomerId != replaceCustomer.Id)
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
                _logger.LogError(ex, "Error replacing hall for booking {BookingId}", bookingId);
                return Error<BookingDto>("An error occurred processing your request. Please try again.", 500);
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

                // Verify the booking belongs to the customer (resolve Customer entity ID)
                var vendorReplaceCustomer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                if (vendorReplaceCustomer == null || booking.CustomerId != vendorReplaceCustomer.Id)
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
                _logger.LogError(ex, "Error replacing vendor for booking {BookingId}", bookingId);
                return Error<BookingDto>("An error occurred processing your request. Please try again.", 500);
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

                // Resolve Customer entity ID from AppUser ID
                var createCustomer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                if (createCustomer == null)
                {
                    return Error<BookingDto>("Customer profile not found", 404);
                }
                bookingDto.CustomerId = createCustomer.Id;

                var bookingEntity = _mapper.Map<HallApp.Core.Entities.BookingEntities.Booking>(bookingDto);

                // Use transactional booking creation with pessimistic locking to prevent race conditions
                var (success, booking, errorMessage) = await _bookingService.CreateBookingWithLockingAsync(bookingEntity);

                if (!success || booking == null)
                {
                    return Error<BookingDto>(errorMessage, 409); // 409 Conflict for double booking
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
                _logger.LogError(ex, "Error creating booking");
                return Error<BookingDto>("An error occurred processing your request. Please try again.", 500);
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

                // Authorization: Only allow the booking customer, hall manager of the hall, vendor manager, or admin
                var isAuthorized = IsAdmin;

                if (!isAuthorized)
                {
                    // Check if current user is the customer who made the booking
                    var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                    isAuthorized = customer != null && booking.CustomerId == customer.Id;
                }

                // HallManager: check assigned halls
                if (!isAuthorized && User.IsInRole("HallManager"))
                {
                    var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(UserId);
                    isAuthorized = hallManager?.Halls.Any(h => h.ID == booking.HallId) ?? false;
                }

                // HallOrganizationManager: check org ownership of the hall
                if (!isAuthorized && User.IsInRole("HallOrganizationManager"))
                {
                    var org = await _organizationService.GetOrganizationByOwnerId(UserId);
                    if (org != null)
                    {
                        var orgHalls = await _hallService.GetOrganizationHallsAsync(org.Id);
                        isAuthorized = orgHalls?.Any(h => h.ID == booking.HallId) ?? false;
                    }
                }

                if (!isAuthorized && User.IsInRole("VendorManager"))
                {
                    var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(UserId);
                    isAuthorized = booking.VendorBookings?.Any(bv =>
                        vendorManager?.Vendors.Any(v => v.Id == bv.VendorId) ?? false) ?? false;
                }

                if (!isAuthorized)
                {
                    return Error<BookingDto>("You do not have permission to view this booking", 403);
                }

                var bookingDto = _mapper.Map<BookingDto>(booking);
                return Success(bookingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request for {Endpoint}", HttpContext.Request.Path);
                return Error<BookingDto>("An error occurred processing your request. Please try again.", 500);
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
                // Resolve Customer entity ID from AppUser ID
                var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                if (customer == null)
                {
                    return Success(Enumerable.Empty<BookingDto>(), "No bookings found");
                }
                var bookings = await _bookingService.GetBookingsByCustomerIdAsync(customer.Id.ToString());
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);
                return Success(bookingDtos, "Your bookings retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user bookings");
                return Error<IEnumerable<BookingDto>>("An error occurred processing your request. Please try again.", 500);
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
                _logger.LogError(ex, "Error retrieving all bookings");
                return Error<IEnumerable<BookingDto>>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Get bookings by hall (Hall Manager and Admin)
        /// </summary>
        /// <param name="hallId">Hall ID</param>
        /// <returns>List of bookings for the specified hall</returns>
        [Authorize(Roles = "Admin,HallOrganizationManager,HallManager")]
        [HttpGet("hall/{hallId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetBookingsByHall(int hallId)
        {
            try
            {
                // Authorization: Verify HallManager or HallOrganizationManager owns this hall
                if ((User.IsInRole("HallOrganizationManager") || User.IsInRole("HallManager")) && !IsAdmin)
                {
                    var hasAccess = false;

                    // HallOrganizationManager: check organization halls
                    if (User.IsInRole("HallOrganizationManager"))
                    {
                        var org = await _organizationService.GetOrganizationByOwnerId(UserId);
                        if (org != null)
                        {
                            var orgHalls = await _hallService.GetOrganizationHallsAsync(org.Id);
                            hasAccess = orgHalls?.Any(h => h.ID == hallId) == true;
                        }
                    }

                    // Also check direct hall assignments (HallManager link)
                    if (!hasAccess)
                    {
                        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(UserId);
                        hasAccess = hallManager?.Halls?.Any(h => h.ID == hallId) == true;
                    }

                    if (!hasAccess)
                    {
                        _logger.LogWarning(
                            "Access denied: User {UserId} attempted to access bookings for hall {HallId}",
                            UserId, hallId);
                        return Error<IEnumerable<BookingDto>>("You do not have access to bookings for this hall", 403);
                    }
                }

                var bookings = await _bookingService.GetBookingsByHallIdAsync(hallId);
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);
                return Success(bookingDtos, $"Bookings for hall {hallId} retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request for {Endpoint}", HttpContext.Request.Path);
                return Error<IEnumerable<BookingDto>>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Get bookings by vendor (Vendor Manager and Admin)
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <returns>List of bookings for the specified vendor</returns>
        [Authorize(Roles = "Admin,VendorOrganizationManager,VendorManager")]
        [HttpGet("vendor/{vendorId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetBookingsByVendor(int vendorId)
        {
            try
            {
                // Authorization: Verify VendorManager owns this vendor
                if (User.IsInRole("VendorManager") && !IsAdmin)
                {
                    var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(UserId);
                    if (vendorManager == null || !vendorManager.Vendors.Any(v => v.Id == vendorId))
                    {
                        return Error<IEnumerable<BookingDto>>("You do not have access to bookings for this vendor", 403);
                    }
                }

                var bookings = await _bookingService.GetBookingsByVendorIdAsync(vendorId);
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);
                return Success(bookingDtos, $"Bookings for vendor {vendorId} retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request for {Endpoint}", HttpContext.Request.Path);
                return Error<IEnumerable<BookingDto>>("An error occurred processing your request. Please try again.", 500);
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

                // Customers can only access their own bookings (resolve Customer entity ID)
                var custBookingsCustomer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                if (!IsAdmin && (custBookingsCustomer == null || customerIdInt != custBookingsCustomer.Id))
                {
                    return Error<IEnumerable<BookingDto>>("Access denied", 403);
                }

                var bookings = await _bookingService.GetCustomerBookingsAsync(customerId);
                var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);
                return Success(bookingDtos, $"Bookings for customer retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings for customer");
                return Error<IEnumerable<BookingDto>>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Update booking
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="updateDto">Updated booking data</param>
        /// <returns>Updated booking details</returns>
        [Authorize(Roles = "Admin,Customer,HallOrganizationManager,HallManager")]
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

                // Resolve Customer entity ID for ownership check
                var updateCustomer = await _customerService.GetCustomerByAppUserIdAsync(UserId);

                if (IsAdmin)
                {
                    // Admin can update all fields
                    var entityToUpdate = _mapper.Map<HallApp.Core.Entities.BookingEntities.Booking>(updateDto);
                    var updatedEntity = await _bookingService.UpdateBookingAsync(entityToUpdate);
                    updatedBooking = _mapper.Map<BookingDto>(updatedEntity);
                }
                else if (updateCustomer != null && existingBooking.CustomerId == updateCustomer.Id)
                {
                    // Customer can only update limited fields
                    var entityToUpdate = _mapper.Map<HallApp.Core.Entities.BookingEntities.Booking>(updateDto);
                    var updatedEntity = await _bookingService.UpdateCustomerBookingAsync(UserId.ToString(), entityToUpdate);
                    updatedBooking = _mapper.Map<BookingDto>(updatedEntity);
                }
                else if (User.IsInRole("HallOrganizationManager") || User.IsInRole("HallManager"))
                {
                    // HallOrganizationManager / HallManager can update bookings for halls they own
                    var hasAccess = false;

                    // HallOrganizationManager: check organization halls
                    if (User.IsInRole("HallOrganizationManager"))
                    {
                        var org = await _organizationService.GetOrganizationByOwnerId(UserId);
                        if (org != null)
                        {
                            var orgHalls = await _hallService.GetOrganizationHallsAsync(org.Id);
                            hasAccess = orgHalls?.Any(h => h.ID == existingBooking.HallId) == true;
                        }
                    }

                    // Also check direct hall assignments (HallManager link)
                    if (!hasAccess)
                    {
                        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(UserId);
                        hasAccess = hallManager?.Halls?.Any(h => h.ID == existingBooking.HallId) == true;
                    }

                    if (!hasAccess)
                    {
                        _logger.LogWarning(
                            "Access denied: User {UserId} attempted to update booking {BookingId} for hall {HallId}",
                            UserId, id, existingBooking.HallId);
                        return Error<BookingDto>("You do not have permission to update bookings for this hall", 403);
                    }

                    // RBAC-004: Hall managers cannot modify financial fields - preserve original values
                    updateDto.TotalPrice = existingBooking.TotalAmount > 0 ? (double)existingBooking.TotalAmount : 0;
                    updateDto.Tax = existingBooking.TaxAmount > 0 ? (double)existingBooking.TaxAmount : 0;
                    updateDto.Discount = existingBooking.DiscountAmount > 0 ? (double)existingBooking.DiscountAmount : 0;
                    updateDto.Coupon = existingBooking.Coupon ?? string.Empty;
                    updateDto.PaymentMethod = existingBooking.PaymentMethod ?? string.Empty;

                    _logger.LogInformation(
                        "RBAC-004: Hall manager {UserId} updating booking {BookingId} - financial fields preserved from original",
                        UserId, id);

                    var entityToUpdate = _mapper.Map<HallApp.Core.Entities.BookingEntities.Booking>(updateDto);
                    var updatedEntity = await _bookingService.UpdateBookingAsync(entityToUpdate);
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
                _logger.LogError(ex, "Error updating booking {BookingId}", id);
                return Error<BookingDto>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Cancel booking. Rejects cancellation of past events or completed visits.
        /// Authorization: Admin, booking customer, or hall manager/org manager for the hall.
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Success response</returns>
        [Authorize(Roles = "Admin,Customer,HallOrganizationManager,HallManager")]
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

                // HIGH-RBAC: Comprehensive authorization check
                var isCancelAuthorized = IsAdmin;

                if (!isCancelAuthorized && User.IsInRole("Customer"))
                {
                    var cancelCustomer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                    isCancelAuthorized = cancelCustomer != null && existingBooking.CustomerId == cancelCustomer.Id;
                }

                if (!isCancelAuthorized && (User.IsInRole("HallManager") || User.IsInRole("HallOrganizationManager")))
                {
                    // HallOrganizationManager: check organization halls
                    if (User.IsInRole("HallOrganizationManager"))
                    {
                        var org = await _organizationService.GetOrganizationByOwnerId(UserId);
                        if (org != null)
                        {
                            var orgHalls = await _hallService.GetOrganizationHallsAsync(org.Id);
                            isCancelAuthorized = orgHalls?.Any(h => h.ID == existingBooking.HallId) == true;
                        }
                    }

                    // Also check direct hall assignments (HallManager link)
                    if (!isCancelAuthorized)
                    {
                        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(UserId);
                        isCancelAuthorized = hallManager?.Halls?.Any(h => h.ID == existingBooking.HallId) == true;
                    }
                }

                if (!isCancelAuthorized)
                {
                    _logger.LogWarning(
                        "HIGH-RBAC: User {UserId} unauthorized to cancel booking {BookingId}",
                        UserId, id);
                    return Error<string>("You do not have permission to cancel this booking", 403);
                }

                // Business rule: Prevent cancelling bookings for events that have already occurred
                if (existingBooking.EventDate < DateTime.UtcNow)
                {
                    _logger.LogWarning(
                        "Cancellation rejected: Booking {BookingId} event date {EventDate} has already passed. Requested by user {UserId}",
                        id, existingBooking.EventDate, UserId);
                    return Error<string>(
                        "Cannot cancel a booking for an event that has already occurred.", 400);
                }

                // Business rule: Prevent cancelling bookings that have been marked as visit completed
                if (existingBooking.IsVisitCompleted)
                {
                    _logger.LogWarning(
                        "Cancellation rejected: Booking {BookingId} has been marked as visit completed. Requested by user {UserId}",
                        id, UserId);
                    return Error<string>(
                        "Cannot cancel a booking that has been marked as visit completed.", 400);
                }

                // Business rule: Prevent cancelling already cancelled bookings
                if (existingBooking.Status == "Cancelled")
                {
                    return Error<string>("This booking has already been cancelled.", 400);
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
                _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
                return Error<string>("An error occurred processing your request. Please try again.", 500);
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
                _logger.LogError(ex, "Error retrieving booking statistics");
                return Error<BookingStatisticsDto>("An error occurred processing your request. Please try again.", 500);
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
                _logger.LogError(ex, "Error checking hall availability");
                return Error<HallAvailabilityDto>("An error occurred processing your request. Please try again.", 500);
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
                _logger.LogError(ex, "Error getting time slots for hall {HallId}", hallId);
                return Error<object>("An error occurred processing your request. Please try again.", 500);
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
                _logger.LogError(ex, "Error getting available dates for hall {HallId}", hallId);
                return Error<object>("An error occurred processing your request. Please try again.", 500);
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
                _logger.LogError(ex, "Error calculating booking pricing");
                return Error<object>("An error occurred processing your request. Please try again.", 500);
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
                _logger.LogError(ex, "Error validating discount code {DiscountCode}", discountCode);
                return Error<object>("An error occurred processing your request. Please try again.", 500);
            }
        }

    }
}
