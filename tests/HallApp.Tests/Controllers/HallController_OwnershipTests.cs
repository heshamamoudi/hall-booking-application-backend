using AutoMapper;
using FluentAssertions;
using HallApp.Application.DTOs.Halls.Hall;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Tests.Helpers;
using HallApp.Web.Controllers.Hall;
using HallApp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HallApp.Tests.Controllers;

/// <summary>
/// Task #3: Test Hall CRUD Operations for HallManager
/// Validates ownership verification on every mutating endpoint.
/// </summary>
public class HallController_OwnershipTests
{
    // --- shared mocks ---
    private readonly Mock<IHallService> _hallService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IFileUploadService> _fileUploadService = new();
    private readonly Mock<IOrganizationService> _organizationService = new();
    private readonly Mock<ILogger<HallController>> _logger = new();

    // --- actors ---
    private const int ManagerAUserId = 10;
    private const int ManagerBUserId = 20;
    private const int HallOwnedByA = 1;
    private const int HallOwnedByB = 2;

    private HallController CreateController()
    {
        return new HallController(
            _hallService.Object,
            _mapper.Object,
            _fileUploadService.Object,
            _organizationService.Object,
            _logger.Object);
    }

    private void SetupHallOwnership()
    {
        // Manager A owns Hall 1
        _hallService
            .Setup(s => s.GetHallsByManagerAsync(ManagerAUserId.ToString()))
            .ReturnsAsync(new List<Hall>
            {
                new() { ID = HallOwnedByA, Name = "Hall A", Description = "Manager A hall" }
            });

        // Manager B owns Hall 2
        _hallService
            .Setup(s => s.GetHallsByManagerAsync(ManagerBUserId.ToString()))
            .ReturnsAsync(new List<Hall>
            {
                new() { ID = HallOwnedByB, Name = "Hall B", Description = "Manager B hall" }
            });

        // Hall lookup by ID
        _hallService
            .Setup(s => s.GetHallByIdAsync(HallOwnedByA))
            .ReturnsAsync(new Hall { ID = HallOwnedByA, Name = "Hall A", Description = "desc", IsActive = true });

        _hallService
            .Setup(s => s.GetHallByIdAsync(HallOwnedByB))
            .ReturnsAsync(new Hall { ID = HallOwnedByB, Name = "Hall B", Description = "desc", IsActive = true });
    }

    // ==============================
    // GET /api/halls/my-halls
    // ==============================

    [Fact]
    public async Task GetMyHalls_Should_ReturnOnlyAssignedHalls_ForManagerA()
    {
        // Arrange
        SetupHallOwnership();
        _mapper.Setup(m => m.Map<List<HallDto>>(It.IsAny<List<Hall>>()))
            .Returns<List<Hall>>(halls => halls.Select(h => new HallDto { ID = h.ID, Name = h.Name }).ToList());

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.GetMyHalls();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value as ApiResponse<IEnumerable<HallDto>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().HaveCount(1);
        apiResponse.Data!.First().ID.Should().Be(HallOwnedByA);
    }

    [Fact]
    public async Task GetMyHalls_Should_NotReturnHallsBelongingToOtherManagers()
    {
        // Arrange
        SetupHallOwnership();
        _mapper.Setup(m => m.Map<List<HallDto>>(It.IsAny<List<Hall>>()))
            .Returns<List<Hall>>(halls => halls.Select(h => new HallDto { ID = h.ID, Name = h.Name }).ToList());

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerBUserId));

        // Act
        var result = await controller.GetMyHalls();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();

        var apiResponse = okResult!.Value as ApiResponse<IEnumerable<HallDto>>;
        apiResponse!.Data.Should().HaveCount(1);
        apiResponse.Data!.First().ID.Should().Be(HallOwnedByB);
        apiResponse.Data!.Should().NotContain(h => h.ID == HallOwnedByA);
    }

    // ==============================
    // PUT /api/halls/{id} - Update
    // ==============================

    [Fact]
    public async Task UpdateHall_Should_Succeed_WhenManagerOwnsHall()
    {
        // Arrange
        SetupHallOwnership();
        var updatedHall = new Hall { ID = HallOwnedByA, Name = "Updated Hall A", Description = "new desc" };
        _hallService.Setup(s => s.UpdateHallAsync(It.IsAny<Hall>())).ReturnsAsync(updatedHall);
        _mapper.Setup(m => m.Map(It.IsAny<HallUpdateDto>(), It.IsAny<Hall>()));
        _mapper.Setup(m => m.Map<HallDto>(It.IsAny<Hall>()))
            .Returns(new HallDto { ID = HallOwnedByA, Name = "Updated Hall A" });

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.UpdateHall(HallOwnedByA, new HallUpdateDto { Name = "Updated Hall A" });

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Manager A should be able to update their own hall");
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateHall_Should_Return403_WhenManagerDoesNotOwnHall()
    {
        // Arrange
        SetupHallOwnership();
        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act - Manager A tries to update Hall owned by Manager B
        var result = await controller.UpdateHall(HallOwnedByB, new HallUpdateDto { Name = "Hijacked" });

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403, "Manager A must not be able to update Manager B's hall");
    }

    [Fact]
    public async Task UpdateHall_Should_Succeed_WhenAdminUpdatesAnyHall()
    {
        // Arrange
        SetupHallOwnership();
        // Admin bypasses ownership
        _hallService
            .Setup(s => s.GetHallsByManagerAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Hall>());
        var updatedHall = new Hall { ID = HallOwnedByB, Name = "Admin Updated", Description = "desc" };
        _hallService.Setup(s => s.UpdateHallAsync(It.IsAny<Hall>())).ReturnsAsync(updatedHall);
        _mapper.Setup(m => m.Map(It.IsAny<HallUpdateDto>(), It.IsAny<Hall>()));
        _mapper.Setup(m => m.Map<HallDto>(It.IsAny<Hall>()))
            .Returns(new HallDto { ID = HallOwnedByB, Name = "Admin Updated" });

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateAdmin());

        // Act
        var result = await controller.UpdateHall(HallOwnedByB, new HallUpdateDto { Name = "Admin Updated" });

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Admin should bypass ownership check");
    }

    // ==============================
    // DELETE /api/halls/{id}
    // ==============================

    [Fact]
    public async Task DeleteHall_Should_Succeed_WhenManagerOwnsHall()
    {
        // Arrange
        SetupHallOwnership();
        _hallService.Setup(s => s.DeleteHallAsync(HallOwnedByA)).ReturnsAsync(true);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.DeleteHall(HallOwnedByA);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteHall_Should_Return403_WhenManagerDoesNotOwnHall()
    {
        // Arrange
        SetupHallOwnership();
        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act - Manager A tries to delete Manager B's hall
        var result = await controller.DeleteHall(HallOwnedByB);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403, "Manager A must not delete Manager B's hall");
    }

    // ==============================
    // PUT /api/halls/{id}/toggle-active
    // ==============================

    [Fact]
    public async Task ToggleHallActive_Should_Succeed_WhenManagerOwnsHall()
    {
        // Arrange
        SetupHallOwnership();
        _hallService.Setup(s => s.UpdateHallAsync(It.IsAny<Hall>()))
            .ReturnsAsync(new Hall { ID = HallOwnedByA, Name = "Hall A", Description = "desc", IsActive = false });
        _mapper.Setup(m => m.Map<HallDto>(It.IsAny<Hall>()))
            .Returns(new HallDto { ID = HallOwnedByA, Name = "Hall A" });

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.ToggleHallActive(HallOwnedByA, false);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ToggleHallActive_Should_Return403_WhenManagerDoesNotOwnHall()
    {
        // Arrange
        SetupHallOwnership();
        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act - Manager A tries to toggle Manager B's hall
        var result = await controller.ToggleHallActive(HallOwnedByB, false);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403);
    }

    // ==============================
    // Edge Cases
    // ==============================

    [Fact]
    public async Task GetMyHalls_Should_ReturnEmpty_WhenManagerHasNoHalls()
    {
        // Arrange
        _hallService
            .Setup(s => s.GetHallsByManagerAsync("30"))
            .ReturnsAsync(new List<Hall>());
        _mapper.Setup(m => m.Map<List<HallDto>>(It.IsAny<List<Hall>>()))
            .Returns(new List<HallDto>());

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(30));

        // Act
        var result = await controller.GetMyHalls();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<IEnumerable<HallDto>>;
        apiResponse!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateHall_Should_Return404_WhenHallDoesNotExist()
    {
        // Arrange
        const int nonExistentHallId = 9999;
        // Manager claims to own it (would pass ownership), but hall does not exist
        _hallService
            .Setup(s => s.GetHallsByManagerAsync(ManagerAUserId.ToString()))
            .ReturnsAsync(new List<Hall> { new() { ID = nonExistentHallId, Name = "Phantom", Description = "desc" } });
        _hallService
            .Setup(s => s.GetHallByIdAsync(nonExistentHallId))
            .ReturnsAsync((Hall?)null);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.UpdateHall(nonExistentHallId, new HallUpdateDto { Name = "Ghost" });

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteHall_Should_Return404_WhenHallDoesNotExist()
    {
        // Arrange
        const int nonExistentHallId = 9999;
        _hallService
            .Setup(s => s.GetHallsByManagerAsync(ManagerAUserId.ToString()))
            .ReturnsAsync(new List<Hall> { new() { ID = nonExistentHallId, Name = "Phantom", Description = "desc" } });
        _hallService
            .Setup(s => s.GetHallByIdAsync(nonExistentHallId))
            .ReturnsAsync((Hall?)null);

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.DeleteHall(nonExistentHallId);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateHall_Should_PreserveAdminOnlyFlags_ForHallManager()
    {
        // Arrange
        SetupHallOwnership();
        var existingHall = new Hall
        {
            ID = HallOwnedByA, Name = "Hall A", Description = "desc",
            IsFeatured = true, IsPremium = true, HasSpecialOffer = true
        };
        _hallService.Setup(s => s.GetHallByIdAsync(HallOwnedByA)).ReturnsAsync(existingHall);
        _hallService.Setup(s => s.UpdateHallAsync(It.IsAny<Hall>()))
            .ReturnsAsync((Hall h) => h);
        _mapper.Setup(m => m.Map(It.IsAny<HallUpdateDto>(), It.IsAny<Hall>()))
            .Callback<HallUpdateDto, Hall>((dto, hall) =>
            {
                hall.Name = dto.Name ?? hall.Name;
                // Simulate mapper resetting the flags
                hall.IsFeatured = false;
                hall.IsPremium = false;
                hall.HasSpecialOffer = false;
            });
        _mapper.Setup(m => m.Map<HallDto>(It.IsAny<Hall>()))
            .Returns<Hall>(h => new HallDto
            {
                ID = h.ID,
                IsFeatured = h.IsFeatured,
                IsPremium = h.IsPremium,
                HasSpecialOffer = h.HasSpecialOffer
            });

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.UpdateHall(HallOwnedByA, new HallUpdateDto { Name = "Still A" });

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<HallDto>;
        apiResponse!.Data!.IsFeatured.Should().BeTrue("HallManager should not be able to change IsFeatured");
        apiResponse.Data.IsPremium.Should().BeTrue("HallManager should not be able to change IsPremium");
        apiResponse.Data.HasSpecialOffer.Should().BeTrue("HallManager should not be able to change HasSpecialOffer");
    }
}
