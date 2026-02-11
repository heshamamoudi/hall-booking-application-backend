using AutoMapper;
using FluentAssertions;
using HallApp.Application.DTOs.Booking;
using HallApp.Application.DTOs.Halls.Hall;
using HallApp.Application.DTOs.Invoice;
using HallApp.Application.Services;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Core.Interfaces.IServices;
using HallApp.Tests.Helpers;
using HallApp.Web.Controllers.Booking;
using HallApp.Web.Controllers.Bookings;
using HallApp.Web.Controllers.Hall;
using HallApp.Web.Controllers.Invoice;
using HallApp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HallApp.Tests.Controllers;

/// <summary>
/// Task #9: Test HallManager Data Isolation (Security Validation)
///
/// Tests these attack scenarios (should ALL fail with 403):
/// 1. HallManager A tries to update Hall owned by HallManager B
/// 2. HallManager A tries to delete Hall owned by HallManager B
/// 3. HallManager A tries to approve booking for Hall owned by HallManager B
/// 4. HallManager A tries to view bookings for Hall owned by HallManager B
/// 5. Customer A tries to view another customer's booking by ID
/// </summary>
public class DataIsolation_SecurityTests
{
    // --- shared mocks ---
    private readonly Mock<IHallService> _hallService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IFileUploadService> _fileUploadService = new();
    private readonly Mock<ILogger<HallController>> _hallLogger = new();
    private readonly Mock<IBookingService> _bookingService = new();
    private readonly Mock<IServiceItemService> _serviceItemService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IBookingFinancialService> _financialService = new();
    private readonly Mock<IHallAvailabilityService> _availabilityService = new();
    private readonly Mock<IPriceCalculationService> _priceCalculationService = new();
    private readonly Mock<ICustomerService> _customerService = new();
    private readonly Mock<IHallManagerService> _hallManagerService = new();
    private readonly Mock<IVendorManagerService> _vendorManagerService = new();
    private readonly Mock<ILogger<BookingController>> _bookingLogger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<ILogger<BookingApprovalController>> _approvalLogger = new();
    private readonly Mock<IInvoiceService> _invoiceService = new();
    private readonly Mock<ILogger<InvoiceController>> _invoiceLogger = new();

    // --- actors ---
    private const int ManagerA_UserId = 10;
    private const int ManagerB_UserId = 20;
    private const int CustomerA_UserId = 30;
    private const int CustomerA_EntityId = 100;
    private const int CustomerB_UserId = 40;
    private const int CustomerB_EntityId = 200;
    private const int Hall_A = 1;
    private const int Hall_B = 2;

    public DataIsolation_SecurityTests()
    {
        _unitOfWork.Setup(u => u.BookingRepository).Returns(_bookingRepository.Object);
    }

    private void SetupFullIsolation()
    {
        // ---- Hall ownership via IHallService (used by HallController.UserOwnsHall) ----
        _hallService
            .Setup(s => s.GetHallsByManagerAsync(ManagerA_UserId.ToString()))
            .ReturnsAsync(new List<Hall> { new() { ID = Hall_A, Name = "Hall A", Description = "desc" } });

        _hallService
            .Setup(s => s.GetHallsByManagerAsync(ManagerB_UserId.ToString()))
            .ReturnsAsync(new List<Hall> { new() { ID = Hall_B, Name = "Hall B", Description = "desc" } });

        // ---- Hall Manager entities (used by BookingController/BookingApprovalController) ----
        _hallManagerService
            .Setup(s => s.GetHallManagerByAppUserIdAsync(ManagerA_UserId))
            .ReturnsAsync(new HallManager
            {
                Id = 1, AppUserId = ManagerA_UserId,
                Halls = new List<Hall> { new() { ID = Hall_A, Name = "Hall A", Description = "desc" } }
            });

        _hallManagerService
            .Setup(s => s.GetHallManagerByAppUserIdAsync(ManagerB_UserId))
            .ReturnsAsync(new HallManager
            {
                Id = 2, AppUserId = ManagerB_UserId,
                Halls = new List<Hall> { new() { ID = Hall_B, Name = "Hall B", Description = "desc" } }
            });

        // ---- Hall lookups ----
        _hallService.Setup(s => s.GetHallByIdAsync(Hall_A))
            .ReturnsAsync(new Hall { ID = Hall_A, Name = "Hall A", Description = "desc", IsActive = true });
        _hallService.Setup(s => s.GetHallByIdAsync(Hall_B))
            .ReturnsAsync(new Hall { ID = Hall_B, Name = "Hall B", Description = "desc", IsActive = true });

        // ---- Customer entities ----
        _customerService
            .Setup(s => s.GetCustomerByAppUserIdAsync(CustomerA_UserId))
            .ReturnsAsync(new Customer { Id = CustomerA_EntityId, AppUserId = CustomerA_UserId });

        _customerService
            .Setup(s => s.GetCustomerByAppUserIdAsync(CustomerB_UserId))
            .ReturnsAsync(new Customer { Id = CustomerB_EntityId, AppUserId = CustomerB_UserId });
    }

    // ===============================================================
    // ATTACK SCENARIO 1: HallManager A tries to UPDATE Hall B
    // ===============================================================

    [Fact]
    public async Task Attack1_ManagerA_UpdateHallB_Should_Return403()
    {
        // Arrange
        SetupFullIsolation();
        var hallController = new HallController(_hallService.Object, _mapper.Object, _fileUploadService.Object, _hallLogger.Object);
        ControllerTestSetup.SetUser(hallController, TestClaimsPrincipalBuilder.CreateHallManager(ManagerA_UserId));

        // Act
        var result = await hallController.UpdateHall(Hall_B, new HallUpdateDto { Name = "HACKED" });

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403,
            "ATTACK BLOCKED: Manager A must NOT update Manager B's hall");
    }

    // ===============================================================
    // ATTACK SCENARIO 2: HallManager A tries to DELETE Hall B
    // ===============================================================

    [Fact]
    public async Task Attack2_ManagerA_DeleteHallB_Should_Return403()
    {
        // Arrange
        SetupFullIsolation();
        var hallController = new HallController(_hallService.Object, _mapper.Object, _fileUploadService.Object, _hallLogger.Object);
        ControllerTestSetup.SetUser(hallController, TestClaimsPrincipalBuilder.CreateHallManager(ManagerA_UserId));

        // Act
        var result = await hallController.DeleteHall(Hall_B);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403,
            "ATTACK BLOCKED: Manager A must NOT delete Manager B's hall");
    }

    // ===============================================================
    // ATTACK SCENARIO 3: HallManager A tries to APPROVE booking for Hall B
    // ===============================================================

    [Fact]
    public async Task Attack3_ManagerA_ApproveBookingForHallB_Should_Return403()
    {
        // Arrange
        SetupFullIsolation();
        var bookingForHallB = new Booking
        {
            Id = 5, HallId = Hall_B, CustomerId = CustomerA_EntityId,
            Status = "Pending", VendorBookings = new List<VendorBooking>()
        };
        _bookingRepository.Setup(r => r.GetBookingWithDetailsAsync(5)).ReturnsAsync(bookingForHallB);

        var approvalController = new BookingApprovalController(_unitOfWork.Object, _hallManagerService.Object, _vendorManagerService.Object, _approvalLogger.Object);
        ControllerTestSetup.SetUser(approvalController, TestClaimsPrincipalBuilder.CreateHallManager(ManagerA_UserId));

        // Act
        var result = await approvalController.HallApproval(5, new HallApprovalRequestDto { Approved = true });

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403,
            "ATTACK BLOCKED: Manager A must NOT approve bookings for Manager B's hall");
    }

    // ===============================================================
    // ATTACK SCENARIO 4: HallManager A tries to VIEW bookings for Hall B
    // ===============================================================

    [Fact]
    public async Task Attack4_ManagerA_ViewBookingsForHallB_Should_Return403()
    {
        // Arrange
        SetupFullIsolation();
        var bookingController = new BookingController(
            _bookingService.Object, _mapper.Object, _serviceItemService.Object,
            _notificationService.Object, _financialService.Object, _availabilityService.Object,
            _priceCalculationService.Object, _customerService.Object, _hallService.Object,
            _hallManagerService.Object, _vendorManagerService.Object, _bookingLogger.Object);
        ControllerTestSetup.SetUser(bookingController, TestClaimsPrincipalBuilder.CreateHallManager(ManagerA_UserId));

        // Act
        var result = await bookingController.GetBookingsByHall(Hall_B);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403,
            "ATTACK BLOCKED: Manager A must NOT view bookings for Manager B's hall");
    }

    // ===============================================================
    // ATTACK SCENARIO 5: Customer A tries to view Customer B's booking by ID
    // ===============================================================

    [Fact]
    public async Task Attack5_CustomerA_ViewCustomerBBooking_Should_Return403()
    {
        // Arrange
        SetupFullIsolation();
        var customerBBooking = new Booking
        {
            Id = 10, HallId = Hall_A, CustomerId = CustomerB_EntityId,
            Status = "Pending", VendorBookings = new List<VendorBooking>()
        };
        _bookingService.Setup(s => s.GetBookingByIdAsync(10)).ReturnsAsync(customerBBooking);

        var bookingController = new BookingController(
            _bookingService.Object, _mapper.Object, _serviceItemService.Object,
            _notificationService.Object, _financialService.Object, _availabilityService.Object,
            _priceCalculationService.Object, _customerService.Object, _hallService.Object,
            _hallManagerService.Object, _vendorManagerService.Object, _bookingLogger.Object);
        ControllerTestSetup.SetUser(bookingController, TestClaimsPrincipalBuilder.CreateCustomer(CustomerA_UserId));

        // Act
        var result = await bookingController.GetBookingById(10);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403,
            "ATTACK BLOCKED: Customer A must NOT view Customer B's booking");
    }

    // ===============================================================
    // ADDITIONAL ATTACK SCENARIOS
    // ===============================================================

    [Fact]
    public async Task Attack6_ManagerA_ToggleActiveHallB_Should_Return403()
    {
        // Arrange
        SetupFullIsolation();
        var hallController = new HallController(_hallService.Object, _mapper.Object, _fileUploadService.Object, _hallLogger.Object);
        ControllerTestSetup.SetUser(hallController, TestClaimsPrincipalBuilder.CreateHallManager(ManagerA_UserId));

        // Act
        var result = await hallController.ToggleHallActive(Hall_B, false);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403,
            "ATTACK BLOCKED: Manager A must NOT toggle active status on Manager B's hall");
    }

    [Fact]
    public async Task Attack7_ManagerA_RejectBookingForHallB_Should_Return403()
    {
        // Arrange
        SetupFullIsolation();
        var bookingForHallB = new Booking
        {
            Id = 5, HallId = Hall_B, CustomerId = CustomerA_EntityId,
            Status = "Pending", VendorBookings = new List<VendorBooking>()
        };
        _bookingRepository.Setup(r => r.GetBookingWithDetailsAsync(5)).ReturnsAsync(bookingForHallB);

        var approvalController = new BookingApprovalController(_unitOfWork.Object, _hallManagerService.Object, _vendorManagerService.Object, _approvalLogger.Object);
        ControllerTestSetup.SetUser(approvalController, TestClaimsPrincipalBuilder.CreateHallManager(ManagerA_UserId));

        // Act
        var result = await approvalController.HallApproval(5, new HallApprovalRequestDto
        {
            Approved = false,
            RejectionReason = "Malicious rejection"
        });

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403,
            "ATTACK BLOCKED: Manager A must NOT reject bookings for Manager B's hall");
    }

    [Fact]
    public async Task Attack8_CustomerA_CancelCustomerBBooking_Should_Return403()
    {
        // Arrange
        SetupFullIsolation();
        var customerBBooking = new Booking
        {
            Id = 10, HallId = Hall_A, CustomerId = CustomerB_EntityId,
            Status = "Pending", VendorBookings = new List<VendorBooking>()
        };
        _bookingService.Setup(s => s.GetBookingByIdAsync(10)).ReturnsAsync(customerBBooking);

        var bookingController = new BookingController(
            _bookingService.Object, _mapper.Object, _serviceItemService.Object,
            _notificationService.Object, _financialService.Object, _availabilityService.Object,
            _priceCalculationService.Object, _customerService.Object, _hallService.Object,
            _hallManagerService.Object, _vendorManagerService.Object, _bookingLogger.Object);
        ControllerTestSetup.SetUser(bookingController, TestClaimsPrincipalBuilder.CreateCustomer(CustomerA_UserId));

        // Act
        var result = await bookingController.CancelBooking(10);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403,
            "ATTACK BLOCKED: Customer A must NOT cancel Customer B's booking");
    }

    [Fact]
    public async Task Attack9_CustomerA_ViewCustomerBInvoice_Should_Return403()
    {
        // Arrange
        SetupFullIsolation();
        var customerBInvoice = new Invoice
        {
            Id = 1, InvoiceNumber = "INV-2025-000001",
            CustomerId = CustomerB_UserId, HallId = Hall_A
        };
        _invoiceService.Setup(s => s.GetInvoiceByIdAsync(1)).ReturnsAsync(customerBInvoice);

        var invoiceController = new InvoiceController(_invoiceService.Object, _mapper.Object, _invoiceLogger.Object);
        ControllerTestSetup.SetUser(invoiceController, TestClaimsPrincipalBuilder.CreateCustomer(CustomerA_UserId));

        // Act
        var result = await invoiceController.GetInvoiceById(1);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403,
            "ATTACK BLOCKED: Customer A must NOT access Customer B's invoice");
    }

    // ===============================================================
    // POSITIVE CONTROL: Admin should bypass all isolation checks
    // ===============================================================

    [Fact]
    public async Task Admin_Should_BypassAllIsolation_ForHallUpdate()
    {
        // Arrange
        SetupFullIsolation();
        _hallService.Setup(s => s.UpdateHallAsync(It.IsAny<Hall>()))
            .ReturnsAsync(new Hall { ID = Hall_B, Name = "Admin Updated", Description = "desc" });
        _mapper.Setup(m => m.Map(It.IsAny<HallUpdateDto>(), It.IsAny<Hall>()));
        _mapper.Setup(m => m.Map<HallDto>(It.IsAny<Hall>()))
            .Returns(new HallDto { ID = Hall_B, Name = "Admin Updated" });

        var hallController = new HallController(_hallService.Object, _mapper.Object, _fileUploadService.Object, _hallLogger.Object);
        ControllerTestSetup.SetUser(hallController, TestClaimsPrincipalBuilder.CreateAdmin());

        // Act
        var result = await hallController.UpdateHall(Hall_B, new HallUpdateDto { Name = "Admin Updated" });

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Admin should bypass ownership checks");
    }

    [Fact]
    public async Task Admin_Should_BypassAllIsolation_ForBookingApproval()
    {
        // Arrange
        SetupFullIsolation();
        var booking = new Booking
        {
            Id = 5, HallId = Hall_B, Status = "Pending",
            VendorBookings = new List<VendorBooking>()
        };
        _bookingRepository.Setup(r => r.GetBookingWithDetailsAsync(5)).ReturnsAsync(booking);
        _unitOfWork.Setup(u => u.Complete()).ReturnsAsync(1);

        var approvalController = new BookingApprovalController(_unitOfWork.Object, _hallManagerService.Object, _vendorManagerService.Object, _approvalLogger.Object);
        ControllerTestSetup.SetUser(approvalController, TestClaimsPrincipalBuilder.CreateAdmin());

        // Act
        var result = await approvalController.HallApproval(5, new HallApprovalRequestDto { Approved = true });

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Admin should bypass ownership checks");
    }
}
