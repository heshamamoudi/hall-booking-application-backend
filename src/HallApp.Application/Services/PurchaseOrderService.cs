using HallApp.Core.Entities;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlatformSettingsService _platformSettingsService;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<PurchaseOrderService> _logger;
    private const decimal TAX_RATE = 0.15m;

    public PurchaseOrderService(
        IUnitOfWork unitOfWork,
        IPlatformSettingsService platformSettingsService,
        IOrganizationService organizationService,
        ILogger<PurchaseOrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _platformSettingsService = platformSettingsService;
        _organizationService = organizationService;
        _logger = logger;
    }

    public async Task<List<PurchaseOrder>> GeneratePurchaseOrdersForInvoiceAsync(int invoiceId, string createdBy)
    {
        var invoice = await _unitOfWork.InvoiceRepository.GetByIdAsync(invoiceId);
        if (invoice == null)
            throw new InvalidOperationException($"Invoice {invoiceId} not found");

        var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(invoice.BookingId);
        if (booking == null)
            throw new InvalidOperationException($"Booking {invoice.BookingId} not found");

        var platformSettings = await _platformSettingsService.GetSettingsAsync();
        var purchaseOrders = new List<PurchaseOrder>();
        var year = DateTime.UtcNow.Year;

        // PO for Hall
        if (booking.HallCost > 0 && booking.Hall != null)
        {
            Organization? hallOrg = null;
            if (booking.Hall.OrganizationId.HasValue)
                hallOrg = await _organizationService.GetOrganizationById(booking.Hall.OrganizationId.Value);

            var commissionRate = _platformSettingsService.GetEffectiveCommissionRate(hallOrg, "Hall", platformSettings);
            var grossAmount = booking.HallCost;
            var commissionAmount = Math.Round(grossAmount * commissionRate, 2);
            var netAmount = grossAmount - commissionAmount;
            var taxAmount = Math.Round(netAmount * TAX_RATE, 2);
            var totalAmount = netAmount + taxAmount;

            var seqNum = await _unitOfWork.PurchaseOrderRepository.GetNextSequenceValueAsync();
            var po = new PurchaseOrder
            {
                PurchaseOrderNumber = $"PO-{year}-{seqNum:D6}",
                InvoiceId = invoiceId,
                BookingId = booking.Id,
                SupplierType = "Hall",
                SupplierId = booking.Hall.ID,
                SupplierOrganizationId = hallOrg?.Id,
                SupplierName = hallOrg?.LegalName ?? hallOrg?.Name ?? booking.Hall.Name ?? "Unknown",
                SupplierVatNumber = hallOrg?.VatNumber ?? string.Empty,
                SupplierCommercialRegistrationNumber = hallOrg?.CommercialRegistrationNumber ?? string.Empty,
                SupplierAddress = BuildSupplierAddress(hallOrg),
                SupplierBankIban = hallOrg?.BankIban ?? string.Empty,
                SupplierBankName = hallOrg?.BankName ?? string.Empty,
                GrossAmount = grossAmount,
                CommissionRate = commissionRate,
                CommissionAmount = commissionAmount,
                NetAmount = netAmount,
                TaxRate = TAX_RATE,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                Status = "Draft",
                CreatedBy = createdBy
            };

            await _unitOfWork.PurchaseOrderRepository.AddAsync(po);
            purchaseOrders.Add(po);
        }

        // POs for Vendors
        if (booking.VendorBookings != null)
        {
            foreach (var vb in booking.VendorBookings.Where(v => v.Status != "Rejected" && v.Status != "Cancelled"))
            {
                Organization? vendorOrg = null;
                if (vb.Vendor?.OrganizationId != null)
                    vendorOrg = await _organizationService.GetOrganizationById(vb.Vendor.OrganizationId.Value);

                var commissionRate = _platformSettingsService.GetEffectiveCommissionRate(vendorOrg, "Vendor", platformSettings);
                var grossAmount = vb.TotalAmount;
                var commissionAmount = Math.Round(grossAmount * commissionRate, 2);
                var netAmount = grossAmount - commissionAmount;
                var taxAmount = Math.Round(netAmount * TAX_RATE, 2);
                var totalAmount = netAmount + taxAmount;

                var seqNum = await _unitOfWork.PurchaseOrderRepository.GetNextSequenceValueAsync();
                var po = new PurchaseOrder
                {
                    PurchaseOrderNumber = $"PO-{year}-{seqNum:D6}",
                    InvoiceId = invoiceId,
                    BookingId = booking.Id,
                    SupplierType = "Vendor",
                    SupplierId = vb.VendorId,
                    SupplierOrganizationId = vendorOrg?.Id,
                    SupplierName = vendorOrg?.LegalName ?? vendorOrg?.Name ?? vb.Vendor?.Name ?? "Unknown",
                    SupplierVatNumber = vendorOrg?.VatNumber ?? string.Empty,
                    SupplierCommercialRegistrationNumber = vendorOrg?.CommercialRegistrationNumber ?? string.Empty,
                    SupplierAddress = BuildSupplierAddress(vendorOrg),
                    SupplierBankIban = vendorOrg?.BankIban ?? string.Empty,
                    SupplierBankName = vendorOrg?.BankName ?? string.Empty,
                    GrossAmount = grossAmount,
                    CommissionRate = commissionRate,
                    CommissionAmount = commissionAmount,
                    NetAmount = netAmount,
                    TaxRate = TAX_RATE,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    Status = "Draft",
                    CreatedBy = createdBy
                };

                await _unitOfWork.PurchaseOrderRepository.AddAsync(po);
                purchaseOrders.Add(po);
            }
        }

        await _unitOfWork.Complete();

        _logger.LogInformation(
            "Generated {Count} purchase orders for invoice {InvoiceId} (booking {BookingId})",
            purchaseOrders.Count, invoiceId, booking.Id);

        return purchaseOrders;
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
        => await _unitOfWork.PurchaseOrderRepository.GetByIdAsync(id);

    public async Task<IEnumerable<PurchaseOrder>> GetByInvoiceIdAsync(int invoiceId)
        => await _unitOfWork.PurchaseOrderRepository.GetByInvoiceIdAsync(invoiceId);

    public async Task<IEnumerable<PurchaseOrder>> GetByBookingIdAsync(int bookingId)
        => await _unitOfWork.PurchaseOrderRepository.GetByBookingIdAsync(bookingId);

    public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
        => await _unitOfWork.PurchaseOrderRepository.GetAllAsync();

    public async Task<PurchaseOrder> UpdateStatusAsync(int id, string status, string updatedBy)
    {
        var po = await _unitOfWork.PurchaseOrderRepository.GetByIdAsync(id);
        if (po == null)
            throw new InvalidOperationException($"Purchase order {id} not found");

        po.Status = status;
        po.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.PurchaseOrderRepository.Update(po);
        await _unitOfWork.Complete();

        _logger.LogInformation("Purchase order {PoId} status updated to {Status} by {User}", id, status, updatedBy);
        return po;
    }

    public async Task<PurchaseOrder> RecordPaymentAsync(int id, string paymentReference, string updatedBy)
    {
        var po = await _unitOfWork.PurchaseOrderRepository.GetByIdAsync(id);
        if (po == null)
            throw new InvalidOperationException($"Purchase order {id} not found");

        po.Status = "Paid";
        po.PaymentDate = DateTime.UtcNow;
        po.PaymentReference = paymentReference;
        po.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.PurchaseOrderRepository.Update(po);
        await _unitOfWork.Complete();

        _logger.LogInformation("Purchase order {PoId} payment recorded: {Reference} by {User}", id, paymentReference, updatedBy);
        return po;
    }

    private static string BuildSupplierAddress(Organization? org)
    {
        if (org == null) return string.Empty;
        var parts = new[]
        {
            org.BuildingNumber, org.StreetName, org.District,
            org.City, org.PostalCode, org.CountryCode
        }.Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }
}
