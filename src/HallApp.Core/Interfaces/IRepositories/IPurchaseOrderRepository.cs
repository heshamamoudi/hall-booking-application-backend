using HallApp.Core.Entities.BookingEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(int id);
    Task<IEnumerable<PurchaseOrder>> GetByInvoiceIdAsync(int invoiceId);
    Task<IEnumerable<PurchaseOrder>> GetByBookingIdAsync(int bookingId);
    Task<IEnumerable<PurchaseOrder>> GetAllAsync();
    Task<PurchaseOrder> AddAsync(PurchaseOrder entity);
    void Update(PurchaseOrder entity);
    Task<int> GetNextSequenceValueAsync();
}
