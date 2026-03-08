using HallApp.Core.Entities.BookingEntities;

namespace HallApp.Core.Interfaces.IServices;

public interface IPurchaseOrderService
{
    Task<List<PurchaseOrder>> GeneratePurchaseOrdersForInvoiceAsync(int invoiceId, string createdBy);
    Task<PurchaseOrder?> GetByIdAsync(int id);
    Task<IEnumerable<PurchaseOrder>> GetByInvoiceIdAsync(int invoiceId);
    Task<IEnumerable<PurchaseOrder>> GetByBookingIdAsync(int bookingId);
    Task<IEnumerable<PurchaseOrder>> GetAllAsync();
    Task<PurchaseOrder> UpdateStatusAsync(int id, string status, string updatedBy);
    Task<PurchaseOrder> RecordPaymentAsync(int id, string paymentReference, string updatedBy);
}
