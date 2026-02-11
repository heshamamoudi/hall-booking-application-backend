using FluentAssertions;
using HallApp.Application.DTOs.Booking;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Core.Interfaces.IServices;
using HallApp.Tests.Helpers;
using HallApp.Web.Controllers.Bookings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HallApp.Tests.Controllers;

/// <summary>
/// Task #4 (continued): Test Hall Approval for HallManager
/// Critical test: HallManager A tries to approve a booking for HallManager B's hall -> should get 403
/// </summary>
public class BookingApprovalController_OwnershipTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IHallManagerService> _hallManagerService = new();
    private readonly Mock<IVendorManagerService> _vendorManagerService = new();
    private readonly Mock<ILogger<BookingApprovalController>> _logger = new();

    private const int ManagerAUserId = 10;
    private const int ManagerBUserId = 20;
    private const int HallOwnedByA = 1;
    private const int HallOwnedByB = 2;

    public BookingApprovalController_OwnershipTests()
    {
        _unitOfWork.Setup(u => u.BookingRepository).Returns(_bookingRepository.Object);
    }

    private BookingApprovalController CreateController()
    {
        return new BookingApprovalController(
            _unitOfWork.Object,
            _hallManagerService.Object,
            _vendorManagerService.Object,
            _logger.Object);
    }

    private void SetupHallOwnership()
    {
        // Manager A owns Hall 1
        _hallManagerService
            .Setup(s => s.GetHallManagerByAppUserIdAsync(ManagerAUserId))
            .ReturnsAsync(new HallManager
            {
                Id = 1,
                AppUserId = ManagerAUserId,
                Halls = new List<Hall> { new() { ID = HallOwnedByA, Name = "Hall A", Description = "desc" } }
            });

        // Manager B owns Hall 2
        _hallManagerService
            .Setup(s => s.GetHallManagerByAppUserIdAsync(ManagerBUserId))
            .ReturnsAsync(new HallManager
            {
                Id = 2,
                AppUserId = ManagerBUserId,
                Halls = new List<Hall> { new() { ID = HallOwnedByB, Name = "Hall B", Description = "desc" } }
            });
    }

    // ==============================
    // POST /api/bookings/{id}/hall-approval
    // ==============================

    [Fact]
    public async Task HallApproval_Should_Succeed_WhenManagerOwnsHall()
    {
        // Arrange
        SetupHallOwnership();
        var booking = new Booking
        {
            Id = 1, HallId = HallOwnedByA, Status = "Pending",
            VendorBookings = new List<VendorBooking>()
        };
        _bookingRepository.Setup(r => r.GetBookingWithDetailsAsync(1)).ReturnsAsync(booking);
        _unitOfWork.Setup(u => u.Complete()).ReturnsAsync(1);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.HallApproval(1, new HallApprovalRequestDto { Approved = true });

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Manager A should approve bookings for their own hall");

        var apiResponse = okResult!.Value as ApiResponse<ApprovalResponseDto>;
        apiResponse!.Data!.Success.Should().BeTrue();
        apiResponse.Data.NewStatus.Should().Be("ReadyForPayment");
    }

    [Fact]
    public async Task HallApproval_Should_Return403_WhenManagerDoesNotOwnHall()
    {
        // Arrange
        SetupHallOwnership();
        var booking = new Booking
        {
            Id = 2, HallId = HallOwnedByB, Status = "Pending",
            VendorBookings = new List<VendorBooking>()
        };
        _bookingRepository.Setup(r => r.GetBookingWithDetailsAsync(2)).ReturnsAsync(booking);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act - CRITICAL: Manager A tries to approve booking for Manager B's hall
        var result = await controller.HallApproval(2, new HallApprovalRequestDto { Approved = true });

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403,
            "SECURITY: Manager A must NOT be able to approve bookings for Manager B's hall");
    }

    [Fact]
    public async Task HallApproval_Reject_Should_Return403_WhenManagerDoesNotOwnHall()
    {
        // Arrange
        SetupHallOwnership();
        var booking = new Booking
        {
            Id = 2, HallId = HallOwnedByB, Status = "Pending",
            VendorBookings = new List<VendorBooking>()
        };
        _bookingRepository.Setup(r => r.GetBookingWithDetailsAsync(2)).ReturnsAsync(booking);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act - Manager A tries to reject booking for Manager B's hall
        var result = await controller.HallApproval(2, new HallApprovalRequestDto
        {
            Approved = false,
            RejectionReason = "I want to sabotage competitor"
        });

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403,
            "SECURITY: Manager A must NOT be able to reject bookings for Manager B's hall");
    }

    [Fact]
    public async Task HallApproval_Should_Succeed_WhenAdminApprovesAnyHall()
    {
        // Arrange
        SetupHallOwnership();
        var booking = new Booking
        {
            Id = 2, HallId = HallOwnedByB, Status = "Pending",
            VendorBookings = new List<VendorBooking>()
        };
        _bookingRepository.Setup(r => r.GetBookingWithDetailsAsync(2)).ReturnsAsync(booking);
        _unitOfWork.Setup(u => u.Complete()).ReturnsAsync(1);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateAdmin());

        // Act
        var result = await controller.HallApproval(2, new HallApprovalRequestDto { Approved = true });

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Admin should be able to approve any booking");
    }

    [Fact]
    public async Task HallApproval_Should_Return404_WhenBookingDoesNotExist()
    {
        // Arrange
        _bookingRepository.Setup(r => r.GetBookingWithDetailsAsync(9999)).ReturnsAsync((Booking?)null);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.HallApproval(9999, new HallApprovalRequestDto { Approved = true });

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task HallApproval_Should_SetVendorsApproving_WhenBookingHasVendorServices()
    {
        // Arrange
        SetupHallOwnership();
        var booking = new Booking
        {
            Id = 1, HallId = HallOwnedByA, Status = "Pending",
            VendorBookings = new List<VendorBooking>
            {
                new() { Id = 1, VendorId = 5, Status = "Pending", Services = new List<VendorBookingService>() }
            }
        };
        _bookingRepository.Setup(r => r.GetBookingWithDetailsAsync(1)).ReturnsAsync(booking);
        _unitOfWork.Setup(u => u.Complete()).ReturnsAsync(1);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.HallApproval(1, new HallApprovalRequestDto { Approved = true });

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<ApprovalResponseDto>;
        apiResponse!.Data!.NewStatus.Should().Be("VendorsApproving",
            "When there are vendor services, status should transition to VendorsApproving, not ReadyForPayment");
        apiResponse.Data.CanProceedToPayment.Should().BeFalse();
    }

    [Fact]
    public async Task HallApproval_Reject_Should_SetHallRejected()
    {
        // Arrange
        SetupHallOwnership();
        var booking = new Booking
        {
            Id = 1, HallId = HallOwnedByA, Status = "Pending",
            VendorBookings = new List<VendorBooking>()
        };
        _bookingRepository.Setup(r => r.GetBookingWithDetailsAsync(1)).ReturnsAsync(booking);
        _unitOfWork.Setup(u => u.Complete()).ReturnsAsync(1);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.HallApproval(1, new HallApprovalRequestDto
        {
            Approved = false,
            RejectionReason = "Date not available"
        });

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<ApprovalResponseDto>;
        apiResponse!.Data!.NewStatus.Should().Be("HallRejected");
        apiResponse.Data.CanProceedToPayment.Should().BeFalse();
    }
}
