using HallApp.Application.DTOs.Admin;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Admin;

/// <summary>
/// Manages purchase orders for supplier payouts.
/// Admin-only access.
/// </summary>
[Authorize(Roles = "Admin")]
[Route("api/v1/purchase-orders")]
public class PurchaseOrderController : BaseApiController
{
    private readonly IPurchaseOrderService _purchaseOrderService;

    public PurchaseOrderController(IPurchaseOrderService purchaseOrderService)
    {
        _purchaseOrderService = purchaseOrderService;
    }

    /// <summary>
    /// Gets all purchase orders.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PurchaseOrderDto>>>> GetAll()
    {
        var purchaseOrders = await _purchaseOrderService.GetAllAsync();
        var dtos = purchaseOrders.Select(MapToDto);
        return Success(dtos);
    }

    /// <summary>
    /// Gets a purchase order by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> GetById(int id)
    {
        var po = await _purchaseOrderService.GetByIdAsync(id);
        if (po == null)
            return Error<PurchaseOrderDto>("Purchase order not found", 404);

        return Success(MapToDto(po));
    }

    /// <summary>
    /// Gets all purchase orders for a specific invoice.
    /// </summary>
    [HttpGet("invoice/{invoiceId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PurchaseOrderDto>>>> GetByInvoiceId(int invoiceId)
    {
        var purchaseOrders = await _purchaseOrderService.GetByInvoiceIdAsync(invoiceId);
        var dtos = purchaseOrders.Select(MapToDto);
        return Success(dtos);
    }

    /// <summary>
    /// Gets all purchase orders for a specific booking.
    /// </summary>
    [HttpGet("booking/{bookingId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PurchaseOrderDto>>>> GetByBookingId(int bookingId)
    {
        var purchaseOrders = await _purchaseOrderService.GetByBookingIdAsync(bookingId);
        var dtos = purchaseOrders.Select(MapToDto);
        return Success(dtos);
    }

    /// <summary>
    /// Generates purchase orders for an invoice.
    /// </summary>
    [HttpPost("generate/{invoiceId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PurchaseOrderDto>>>> GenerateForInvoice(int invoiceId)
    {
        var purchaseOrders = await _purchaseOrderService.GeneratePurchaseOrdersForInvoiceAsync(invoiceId, UserName);
        var dtos = purchaseOrders.Select(MapToDto);
        return Success(dtos, "Purchase orders generated successfully");
    }

    /// <summary>
    /// Updates the status of a purchase order.
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> UpdateStatus(
        int id, [FromBody] UpdatePurchaseOrderStatusDto dto)
    {
        var po = await _purchaseOrderService.UpdateStatusAsync(id, dto.Status, UserName);
        return Success(MapToDto(po), "Purchase order status updated successfully");
    }

    /// <summary>
    /// Records a payment for a purchase order.
    /// </summary>
    [HttpPut("{id}/payment")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> RecordPayment(
        int id, [FromBody] RecordPurchaseOrderPaymentDto dto)
    {
        var po = await _purchaseOrderService.RecordPaymentAsync(id, dto.PaymentReference, UserName);
        return Success(MapToDto(po), "Purchase order payment recorded successfully");
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrder po)
    {
        return new PurchaseOrderDto
        {
            Id = po.Id,
            PurchaseOrderNumber = po.PurchaseOrderNumber,
            InvoiceId = po.InvoiceId,
            InvoiceNumber = po.Invoice?.InvoiceNumber,
            BookingId = po.BookingId,
            SupplierType = po.SupplierType,
            SupplierId = po.SupplierId,
            SupplierName = po.SupplierName,
            SupplierVatNumber = po.SupplierVatNumber,
            SupplierCommercialRegistrationNumber = po.SupplierCommercialRegistrationNumber,
            SupplierAddress = po.SupplierAddress,
            SupplierBankIban = po.SupplierBankIban,
            SupplierBankName = po.SupplierBankName,
            GrossAmount = po.GrossAmount,
            CommissionRate = po.CommissionRate,
            CommissionAmount = po.CommissionAmount,
            NetAmount = po.NetAmount,
            TaxRate = po.TaxRate,
            TaxAmount = po.TaxAmount,
            TotalAmount = po.TotalAmount,
            Currency = po.Currency,
            Status = po.Status,
            PaymentDate = po.PaymentDate,
            PaymentReference = po.PaymentReference,
            CreatedAt = po.CreatedAt,
            CreatedBy = po.CreatedBy
        };
    }
}
