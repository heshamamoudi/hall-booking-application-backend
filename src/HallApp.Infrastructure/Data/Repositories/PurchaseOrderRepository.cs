using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly DataContext _context;

    public PurchaseOrderRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Invoice)
            .Include(po => po.Booking)
            .Include(po => po.SupplierOrganization)
            .FirstOrDefaultAsync(po => po.Id == id);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByInvoiceIdAsync(int invoiceId)
    {
        return await _context.PurchaseOrders
            .Where(po => po.InvoiceId == invoiceId)
            .Include(po => po.SupplierOrganization)
            .OrderBy(po => po.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByBookingIdAsync(int bookingId)
    {
        return await _context.PurchaseOrders
            .Where(po => po.BookingId == bookingId)
            .Include(po => po.SupplierOrganization)
            .Include(po => po.Invoice)
            .OrderBy(po => po.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
    {
        return await _context.PurchaseOrders
            .Include(po => po.Invoice)
            .Include(po => po.SupplierOrganization)
            .OrderByDescending(po => po.CreatedAt)
            .ToListAsync();
    }

    public async Task<PurchaseOrder> AddAsync(PurchaseOrder entity)
    {
        var result = await _context.PurchaseOrders.AddAsync(entity);
        return result.Entity;
    }

    public void Update(PurchaseOrder entity)
    {
        _context.PurchaseOrders.Update(entity);
    }

    public async Task<int> GetNextSequenceValueAsync()
    {
        var isPostgres = _context.Database.ProviderName?.Contains("Npgsql") ?? false;

        if (isPostgres)
        {
            var result = await _context.Database.SqlQueryRaw<int>(
                "SELECT nextval('purchase_order_number_seq')::int AS \"Value\"").FirstAsync();
            return result;
        }
        else
        {
            var result = await _context.Database.SqlQueryRaw<int>(
                "SELECT NEXT VALUE FOR purchase_order_number_seq AS [Value]").FirstAsync();
            return result;
        }
    }
}
