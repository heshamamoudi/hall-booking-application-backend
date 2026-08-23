using AutoMapper;
using FluentAssertions;
using HallApp.Application.DTOs.Invoice;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Tests.Helpers;
using HallApp.Web.Controllers.Invoice;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HallApp.Tests.Controllers;

/// <summary>
/// Task #6: Test Invoice Management for HallManager
/// Validates that invoices are properly scoped by role and hall ownership.
/// </summary>
public class InvoiceController_OwnershipTests
{
    private readonly Mock<IInvoiceService> _invoiceService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ILogger<InvoiceController>> _logger = new();
    private readonly Mock<IHallManagerService> _hallManagerService = new();
    private readonly Mock<IVendorManagerService> _vendorManagerService = new();
    private readonly Mock<IOrganizationService> _organizationService = new();
    private readonly Mock<IHallService> _hallService = new();
    private readonly Mock<IFinancialAuditService> _financialAuditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private const int ManagerAUserId = 10;
    private const int ManagerBUserId = 20;
    private const int CustomerUserId = 30;
    private const int HallOwnedByA = 1;
    private const int HallOwnedByB = 2;

    private InvoiceController CreateController()
    {
        return new InvoiceController(
            _invoiceService.Object,
            _hallManagerService.Object,
            _organizationService.Object,
            _hallService.Object,
            _mapper.Object,
            _logger.Object,
            _financialAuditService.Object,
            _unitOfWork.Object);
    }

    private void SetupInvoiceData()
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

        // GetMyInvoices collects every hall the caller can reach - organization
        // halls plus directly assigned ones - and asks for them in one call, so the
        // mock is on the plural GetInvoicesByHallIdsAsync rather than the singular
        // lookup this test used to stub.
        var invoicesByHall = new Dictionary<int, Invoice>
        {
            [HallOwnedByA] = new() { Id = 1, InvoiceNumber = "INV-2025-000001", BookingId = 1, CustomerId = CustomerUserId, HallId = HallOwnedByA },
            [HallOwnedByB] = new() { Id = 2, InvoiceNumber = "INV-2025-000002", BookingId = 2, CustomerId = CustomerUserId, HallId = HallOwnedByB },
        };

        _invoiceService
            .Setup(s => s.GetInvoicesByHallIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync((List<int> hallIds) => hallIds
                .Where(invoicesByHall.ContainsKey)
                .Select(id => invoicesByHall[id])
                .ToList());

        _invoiceService
            .Setup(s => s.GetInvoicesByHallIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int hallId) => invoicesByHall.TryGetValue(hallId, out var invoice)
                ? new List<Invoice> { invoice }
                : new List<Invoice>());
    }

    // ==============================
    // GET /api/invoice/my-invoices - Role-based scoping
    // ==============================

    [Fact]
    public async Task GetMyInvoices_Should_ReturnOnlyHallAInvoices_ForManagerA()
    {
        // Arrange
        SetupInvoiceData();
        _mapper.Setup(m => m.Map<IEnumerable<InvoiceListDto>>(It.IsAny<IEnumerable<Invoice>>()))
            .Returns<IEnumerable<Invoice>>(invoices =>
                invoices.Select(i => new InvoiceListDto { Id = i.Id, InvoiceNumber = i.InvoiceNumber }).ToList());

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act - Use reflection to inject [FromServices] dependency
        var result = await controller.GetMyInvoices(_hallManagerService.Object, _vendorManagerService.Object);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Manager A should get their invoices");

        var apiResponse = okResult!.Value as ApiResponse<IEnumerable<InvoiceListDto>>;
        apiResponse!.Data.Should().HaveCount(1);
        apiResponse.Data!.First().Id.Should().Be(1, "Manager A should only see Invoice 1 (from Hall A)");
    }

    [Fact]
    public async Task GetMyInvoices_Should_ReturnOnlyHallBInvoices_ForManagerB()
    {
        // Arrange
        SetupInvoiceData();
        _mapper.Setup(m => m.Map<IEnumerable<InvoiceListDto>>(It.IsAny<IEnumerable<Invoice>>()))
            .Returns<IEnumerable<Invoice>>(invoices =>
                invoices.Select(i => new InvoiceListDto { Id = i.Id, InvoiceNumber = i.InvoiceNumber }).ToList());

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerBUserId));

        // Act
        var result = await controller.GetMyInvoices(_hallManagerService.Object, _vendorManagerService.Object);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();

        var apiResponse = okResult!.Value as ApiResponse<IEnumerable<InvoiceListDto>>;
        apiResponse!.Data.Should().HaveCount(1);
        apiResponse.Data!.First().Id.Should().Be(2, "Manager B should only see Invoice 2 (from Hall B)");
    }

    [Fact]
    public async Task GetMyInvoices_Should_ReturnEmpty_WhenManagerHasNoHalls()
    {
        // Arrange
        _hallManagerService
            .Setup(s => s.GetHallManagerByAppUserIdAsync(99))
            .ReturnsAsync(new HallManager { Id = 99, AppUserId = 99, Halls = new List<Hall>() });
        _mapper.Setup(m => m.Map<IEnumerable<InvoiceListDto>>(It.IsAny<IEnumerable<Invoice>>()))
            .Returns(new List<InvoiceListDto>());

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(99));

        // Act
        var result = await controller.GetMyInvoices(_hallManagerService.Object, _vendorManagerService.Object);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<IEnumerable<InvoiceListDto>>;
        apiResponse!.Data.Should().BeEmpty();
    }

    // ==============================
    // GET /api/invoice/{id} - Access control
    // ==============================

    [Fact]
    public async Task GetInvoiceById_Should_Succeed_WhenHallManagerAccessesInvoice()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = 1, InvoiceNumber = "INV-2025-000001",
            CustomerId = CustomerUserId, HallId = HallOwnedByA
        };
        SetupInvoiceData();
        _invoiceService.Setup(s => s.GetInvoiceByIdAsync(1)).ReturnsAsync(invoice);
        _mapper.Setup(m => m.Map<InvoiceDto>(It.IsAny<Invoice>()))
            .Returns(new InvoiceDto { Id = 1, InvoiceNumber = "INV-2025-000001" });

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.GetInvoiceById(1);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("HallManager role grants access to individual invoices (isAdmin check includes HallManager)");
    }

    [Fact]
    public async Task GetInvoiceById_Should_Return403_WhenCustomerAccessesOtherCustomerInvoice()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = 1, InvoiceNumber = "INV-2025-000001",
            CustomerId = CustomerUserId, HallId = HallOwnedByA
        };
        _invoiceService.Setup(s => s.GetInvoiceByIdAsync(1)).ReturnsAsync(invoice);

        var otherCustomerUserId = 50;
        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateCustomer(otherCustomerUserId));

        // Act
        var result = await controller.GetInvoiceById(1);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403, "Customer must not access another customer's invoice");
    }

    [Fact]
    public async Task GetInvoiceById_Should_Return404_WhenInvoiceDoesNotExist()
    {
        // Arrange
        _invoiceService.Setup(s => s.GetInvoiceByIdAsync(9999))
            .ReturnsAsync(new Invoice { Id = 0 }); // Service returns Id=0 for not found

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.GetInvoiceById(9999);

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(404);
    }

    // ==============================
    // GET /api/invoice/{id}/pdf - PDF download
    // ==============================

    [Fact]
    public async Task GetInvoicePdf_Should_Succeed_ForHallManager()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = 1, InvoiceNumber = "INV-2025-000001",
            CustomerId = CustomerUserId, HallId = HallOwnedByA
        };
        // Ownership is checked before the PDF is produced, so the manager's halls
        // have to be wired up or the request is correctly refused.
        SetupInvoiceData();
        _invoiceService.Setup(s => s.GetInvoiceByIdAsync(1)).ReturnsAsync(invoice);
        _invoiceService.Setup(s => s.GetInvoicePdfBytesAsync(1))
            .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF header

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(ManagerAUserId));

        // Act
        var result = await controller.GetInvoicePdf(1);

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = result as FileContentResult;
        fileResult!.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task GetInvoicePdf_Should_ReturnForbid_WhenCustomerAccessesOtherInvoice()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = 1, InvoiceNumber = "INV-2025-000001",
            CustomerId = CustomerUserId, HallId = HallOwnedByA
        };
        _invoiceService.Setup(s => s.GetInvoiceByIdAsync(1)).ReturnsAsync(invoice);

        var otherCustomerUserId = 50;
        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateCustomer(otherCustomerUserId));

        // Act
        var result = await controller.GetInvoicePdf(1);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    // ==============================
    // Invoice scoping - Multiple halls
    // ==============================

    [Fact]
    public async Task GetMyInvoices_Should_ReturnInvoicesFromAllManagedHalls_WhenManagerHasMultipleHalls()
    {
        // Arrange
        const int multiHallManagerUserId = 50;
        const int hallId3 = 3;
        const int hallId4 = 4;

        _hallManagerService
            .Setup(s => s.GetHallManagerByAppUserIdAsync(multiHallManagerUserId))
            .ReturnsAsync(new HallManager
            {
                Id = 5,
                AppUserId = multiHallManagerUserId,
                Halls = new List<Hall>
                {
                    new() { ID = hallId3, Name = "Hall C", Description = "desc" },
                    new() { ID = hallId4, Name = "Hall D", Description = "desc" }
                }
            });

        // One call for every hall the manager can reach, not one call per hall.
        var acrossBothHalls = new List<Invoice>
        {
            new() { Id = 10, InvoiceNumber = "INV-2025-000010", HallId = hallId3, CustomerId = 1 },
            new() { Id = 11, InvoiceNumber = "INV-2025-000011", HallId = hallId3, CustomerId = 2 },
            new() { Id = 12, InvoiceNumber = "INV-2025-000012", HallId = hallId4, CustomerId = 3 }
        };

        _invoiceService
            .Setup(s => s.GetInvoicesByHallIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync((List<int> hallIds) =>
                acrossBothHalls.Where(i => i.HallId.HasValue && hallIds.Contains(i.HallId.Value)).ToList());

        _mapper.Setup(m => m.Map<IEnumerable<InvoiceListDto>>(It.IsAny<IEnumerable<Invoice>>()))
            .Returns<IEnumerable<Invoice>>(invoices =>
                invoices.Select(i => new InvoiceListDto { Id = i.Id, InvoiceNumber = i.InvoiceNumber }).ToList());

        var controller = CreateController();
        ControllerTestSetup.SetUser(controller, TestClaimsPrincipalBuilder.CreateHallManager(multiHallManagerUserId));

        // Act
        var result = await controller.GetMyInvoices(_hallManagerService.Object, _vendorManagerService.Object);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<IEnumerable<InvoiceListDto>>;
        apiResponse!.Data.Should().HaveCount(3, "Manager with 2 halls should see all 3 invoices combined");
    }
}
