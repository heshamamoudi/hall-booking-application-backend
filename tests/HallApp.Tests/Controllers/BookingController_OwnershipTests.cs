using AutoMapper;
using FluentAssertions;
using HallApp.Application.DTOs.Booking;
using HallApp.Application.Services;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Tests.Helpers;
using HallApp.Web.Controllers.Booking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HallApp.Tests.Controllers;

/// <summary>
/// Task #4: Test Booking Management for HallManager
/// Validates ownership checks on booking endpoints and participant verification.
/// </summary>
public class BookingController_OwnershipTests
{
    // --- shared mocks ---
    private readonly Mock<IBookingService> _bookingService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IServiceItemService> _serviceItemService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IBookingFinancialService> _financialService = new();
    private readonly Mock<IHallAvailabilityService> _availabilityService = new();
    private readonly Mock<IPriceCalculationService> _priceCalculationService = new();
    private readonly Mock<ICustomerService> _customerService = new();
    private readonly Mock<IHallService> _hallService = new();
    private readonly Mock<IHallManagerService> _hallManagerService = new();
    private readonly Mock<IVendorManagerService> _vendorManagerService = new();
    private readonly Mock<ILogger<BookingController>> _logger = new();

    // --- actors ---
    private const int ManagerAUserId = 10;
    private const int ManagerBUserId = 20;
    private const int CustomerUserId = 30;
    private const int CustomerEntityId = 100;
    private const int HallOwnedByA = 1;
    private const int HallOwnedByB = 2;

    private BookingController CreateController()
    {
        return new BookingController(
            _bookingService.Object,
            _mapper.Object,
            _serviceItemService.Object,
            _notificationService.Object,
            _financialService.Object,
            _availabilityService.Object,
            _priceCalculationService.Object,
            _customerService.Object,
            _hallService.Object,
            _hallManagerService.Object,
            _vendorManagerService.Object,
            _logger.Object);
    }

    private void SetupStandardData()
    {
        // Manager A owns Hall 1 via HallManager entity
        _hallManagerService
            .Setup(s => s.GetHallManagerByAppUserIdAsync(ManagerAUserId))
            .ReturnsAsync(new HallManager
            {
                Id = 1,
                AppUserId = ManagerAUserId,
                Halls = new List<Hall> { new() { ID = HallOwnedByA, Name = "Hall A", Description = "desc" } }
            });

        // Manager B owns Hall 2 via HallManager entity
        _hallManagerService
            .Setup(s => s.GetHallManagerByAppUserIdAsync(ManagerBUserId))
            .ReturnsAsync(new HallManager
            {
                Id = 2,
                AppUserId = ManagerBUserId,
                Halls = new List<Hall> { new() { ID = HallOwnedByB, Name = "Hall B", Description = "desc" } }
            });

        // Hall ownership via IHallService (used by BookingController.GetBookingsByHall)
        _hallService
            .Setup(s => s.GetHallsByManagerAsync(ManagerAUserId.ToString()))
            .ReturnsAsync(new List<Hall> { new() { ID = HallOwnedByA, Name = "Hall A", Description = "desc" } });

        _hallService
            .Setup(s => s.GetHallsByManagerAsync(ManagerBUserId.ToString()))
            .ReturnsAsync(new List<Hall> { new() { ID = HallOwnedByB, Name = "Hall B", Description = "desc" } });

        // Customer resolution
        _customerService
            .Setup(s => s.GetCustomerByAppUserIdAsync(CustomerUserId))
            .ReturnsAsync(new Customer { Id = CustomerEntityId, AppUserId = CustomerUserId });
    }

    // ==============================
    // GET /api/bookings/hall/{hallId} - Ownership check
    // ==============================

    [Fact]
    public async Task GetBookingsByHall_Should_Succeed_WhenManagerOwnsHall()
    {
        // Arrange
        SetupStandardData();
        var bookings = new List<Booking>
        {
            new() { Id = 1, HallId = HallOwnedByA, CustomerId = CustomerEntityId, Status = "Pending" }
        };
        _bookingService.Setup(s => s.GetBookingsByHallIdAsync(HallOwnedByA)).ReturnsAsync(bookings);
        _mapper.Setup(m => m.Map<IEnumerable<BookingDto>>(It.IsAny<IEnumerable<Booking>>()))
            .Returns(new List<BookingDto> { new() { Id = 1, HallId = HallOwnedByA } });

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.GetBookingsByHall(HallOwnedByA);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Manager A should see bookings for their own hall");
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetBookingsByHall_Should_Return403_WhenManagerDoesNotOwnHall()
    {
        // Arrange
        SetupStandardData();
        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act - Manager A tries to view bookings for Manager B's hall
        var result = await controller.GetBookingsByHall(HallOwnedByB);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403, "Manager A must not see bookings for Manager B's hall");
    }

    // ==============================
    // GET /api/bookings/{id} - Participant check
    // ==============================

    [Fact]
    public async Task GetBookingById_Should_Succeed_WhenHallManagerOwnsBookingHall()
    {
        // Arrange
        SetupStandardData();
        var booking = new Booking
        {
            Id = 1, HallId = HallOwnedByA, CustomerId = CustomerEntityId,
            Status = "Pending", VendorBookings = new List<VendorBooking>()
        };
        _bookingService.Setup(s => s.GetBookingByIdAsync(1)).ReturnsAsync(booking);
        _mapper.Setup(m => m.Map<BookingDto>(It.IsAny<Booking>()))
            .Returns(new BookingDto { Id = 1, HallId = HallOwnedByA });

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.GetBookingById(1);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Manager A should see bookings for their hall");
    }

    [Fact]
    public async Task GetBookingById_Should_Return403_WhenHallManagerDoesNotOwnBookingHall()
    {
        // Arrange
        SetupStandardData();
        var booking = new Booking
        {
            Id = 2, HallId = HallOwnedByB, CustomerId = CustomerEntityId,
            Status = "Pending", VendorBookings = new List<VendorBooking>()
        };
        _bookingService.Setup(s => s.GetBookingByIdAsync(2)).ReturnsAsync(booking);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.GetBookingById(2);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403, "Manager A must not see bookings for Manager B's hall");
    }

    [Fact]
    public async Task GetBookingById_Should_Succeed_WhenCustomerOwnsBooking()
    {
        // Arrange
        SetupStandardData();
        var booking = new Booking
        {
            Id = 1, HallId = HallOwnedByA, CustomerId = CustomerEntityId,
            Status = "Pending", VendorBookings = new List<VendorBooking>()
        };
        _bookingService.Setup(s => s.GetBookingByIdAsync(1)).ReturnsAsync(booking);
        _mapper.Setup(m => m.Map<BookingDto>(It.IsAny<Booking>()))
            .Returns(new BookingDto { Id = 1, HallId = HallOwnedByA });

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateCustomer(CustomerUserId));

        // Act
        var result = await controller.GetBookingById(1);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Customer who made the booking should be able to view it");
    }

    [Fact]
    public async Task GetBookingById_Should_Return403_WhenCustomerDoesNotOwnBooking()
    {
        // Arrange
        SetupStandardData();
        var otherCustomerUserId = 40;
        _customerService
            .Setup(s => s.GetCustomerByAppUserIdAsync(otherCustomerUserId))
            .ReturnsAsync(new Customer { Id = 200, AppUserId = otherCustomerUserId });

        var booking = new Booking
        {
            Id = 1, HallId = HallOwnedByA, CustomerId = CustomerEntityId,
            Status = "Pending", VendorBookings = new List<VendorBooking>()
        };
        _bookingService.Setup(s => s.GetBookingByIdAsync(1)).ReturnsAsync(booking);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateCustomer(otherCustomerUserId));

        // Act - Different customer tries to view booking that belongs to CustomerEntityId (100)
        var result = await controller.GetBookingById(1);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403, "Customer must not see another customer's booking");
    }

    // ==============================
    // Edge Cases
    // ==============================

    [Fact]
    public async Task GetBookingById_Should_Return404_WhenBookingDoesNotExist()
    {
        // Arrange
        _bookingService.Setup(s => s.GetBookingByIdAsync(9999)).ReturnsAsync((Booking?)null);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.GetBookingById(9999);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetBookingById_Should_Return400_WhenIdIsInvalid()
    {
        // Arrange
        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.GetBookingById(0);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetBookingById_Should_Succeed_WhenAdminAccessesAnyBooking()
    {
        // Arrange
        SetupStandardData();
        var booking = new Booking
        {
            Id = 1, HallId = HallOwnedByB, CustomerId = CustomerEntityId,
            Status = "Pending", VendorBookings = new List<VendorBooking>()
        };
        _bookingService.Setup(s => s.GetBookingByIdAsync(1)).ReturnsAsync(booking);
        _mapper.Setup(m => m.Map<BookingDto>(It.IsAny<Booking>()))
            .Returns(new BookingDto { Id = 1, HallId = HallOwnedByB });

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateAdmin());

        // Act
        var result = await controller.GetBookingById(1);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Admin should access any booking");
    }
}
