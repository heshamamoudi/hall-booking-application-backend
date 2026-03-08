using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Invoice operations
/// </summary>
public class InvoiceRepository : GenericRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(DataContext context) : base(context)
    {
    }

    public async Task<Invoice?> GetInvoiceByBookingIdAsync(int bookingId)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
                .ThenInclude(b => b!.Customer)
                    .ThenInclude(c => c!.AppUser)
            .Include(i => i.Booking)
                .ThenInclude(b => b!.Hall)
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.BookingId == bookingId);
    }

    public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByCustomerIdAsync(int customerId)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByHallIdAsync(int hallId)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .Where(i => i.HallId == hallId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    /// <summary>
    /// Batch method to get invoices for multiple halls in a single query.
    /// HIGH-001 FIX: Replaces N+1 parallel Task.WhenAll pattern.
    /// </summary>
    public async Task<IEnumerable<Invoice>> GetInvoicesByHallIdsAsync(IEnumerable<int> hallIds)
    {
        var hallIdsList = hallIds.ToList();

        return await _context.Invoices
            .Include(i => i.Booking)
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .Where(i => i.HallId.HasValue && hallIdsList.Contains(i.HallId.Value))
            .OrderByDescending(i => i.InvoiceDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByVendorIdAsync(int vendorId)
    {
        // Get invoices that contain line items referencing this vendor
        // or invoices where the booking includes vendor services from this vendor
        return await _context.Invoices
            .Include(i => i.Booking)
                .ThenInclude(b => b!.VendorBookings)
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .Where(i => i.Booking != null &&
                        i.Booking.VendorBookings != null &&
                        i.Booking.VendorBookings.Any(vb => vb.VendorId == vendorId))
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByStatusAsync(string paymentStatus)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .Where(i => i.PaymentStatus == paymentStatus)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<int> GetInvoiceCountForYearAsync(int year)
    {
        return await _context.Invoices
            .Where(i => i.InvoiceDate.Year == year)
            .CountAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetNextInvoiceSequenceValueAsync()
    {
        // CRIT-001 FIX: Use database sequence for atomic invoice number generation.
        // Detects the database provider to use the correct SQL syntax.
        var isPostgres = _context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        var sql = isPostgres
            ? "SELECT nextval('invoice_number_seq')"
            : "SELECT NEXT VALUE FOR invoice_number_seq";

        // Use raw SQL to get the next sequence value atomically
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection!.State != System.Data.ConnectionState.Open)
        {
            await command.Connection.OpenAsync();
        }

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<Invoice?> GetInvoiceWithDetailsAsync(int invoiceId)
    {
        return await _context.Invoices
            .Include(i => i.Booking)
                .ThenInclude(b => b!.Customer)
                    .ThenInclude(c => c!.AppUser)
            .Include(i => i.Booking)
                .ThenInclude(b => b!.Hall)
            .Include(i => i.Booking)
                .ThenInclude(b => b!.VendorBookings)
                    .ThenInclude(vb => vb!.Vendor)
            .Include(i => i.Booking)
                .ThenInclude(b => b!.VendorBookings)
                    .ThenInclude(vb => vb!.Services)
                        .ThenInclude(s => s!.ServiceItem)
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    public override async Task<IEnumerable<Invoice>> GetAllAsync()
    {
        return await _context.Invoices
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public override async Task<Invoice?> GetByIdAsync(int id)
    {
        return await GetInvoiceWithDetailsAsync(id);
    }

    /// <inheritdoc />
    public async Task RemoveLineItemsByInvoiceIdAsync(int invoiceId)
    {
        var lineItems = await _context.InvoiceLineItems
            .Where(li => li.InvoiceId == invoiceId)
            .ToListAsync();

        if (lineItems.Any())
        {
            _context.InvoiceLineItems.RemoveRange(lineItems);
        }
    }

    // --- Hall Statistics queries (optimized, read-only) ---

    /// <inheritdoc />
    public async Task<int> GetInvoiceCountByHallIdAsync(int hallId, string? paymentStatus = null)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Where(i => i.HallId == hallId && !i.IsCancelled);

        if (!string.IsNullOrEmpty(paymentStatus))
        {
            query = query.Where(i => i.PaymentStatus == paymentStatus);
        }

        return await query.CountAsync();
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalRevenueByHallIdAsync(int hallId)
    {
        return await _context.Invoices
            .AsNoTracking()
            .Where(i => i.HallId == hallId && i.PaymentStatus == "Paid" && !i.IsCancelled)
            .SumAsync(i => i.TotalAmountWithTax);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Invoice>> GetInvoicesByHallIdAndDateRangeAsync(
        int hallId, DateTime startDate, DateTime endDate)
    {
        return await _context.Invoices
            .AsNoTracking()
            .Where(i => i.HallId == hallId
                     && i.PaymentStatus == "Paid"
                     && !i.IsCancelled
                     && i.InvoiceDate >= startDate
                     && i.InvoiceDate <= endDate)
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync();
    }

    // --- Enhanced query methods for invoice page redesign ---

    /// <inheritdoc />
    public async Task<(List<Invoice> Items, int TotalCount)> GetFilteredInvoicesAsync(
        IEnumerable<int>? hallIds,
        string? search,
        string? status,
        string? paymentMethod,
        int? hallId,
        int? organizationId,
        decimal? minAmount,
        decimal? maxAmount,
        DateTime? startDate,
        DateTime? endDate,
        bool? isCancelled,
        string sortBy,
        bool sortDescending,
        int pageNumber,
        int pageSize)
    {
        var query = _context.Invoices
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .AsNoTracking()
            .AsQueryable();

        // Role-scope filter: restrict to specific hall IDs (null = Admin, no restriction)
        if (hallIds != null)
        {
            var hallIdsList = hallIds.ToList();
            query = query.Where(i => i.HallId.HasValue && hallIdsList.Contains(i.HallId.Value));
        }

        // Organization filter (Admin only): get all halls for the organization
        if (organizationId.HasValue)
        {
            query = query.Where(i => i.Hall != null && i.Hall.OrganizationId == organizationId.Value);
        }

        // Specific hall filter
        if (hallId.HasValue)
        {
            query = query.Where(i => i.HallId == hallId.Value);
        }

        // Search filter: searches invoice number, customer name (BuyerName), hall name, booking ID
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(i =>
                i.InvoiceNumber.ToLower().Contains(searchLower) ||
                i.BuyerName.ToLower().Contains(searchLower) ||
                (i.Hall != null && i.Hall.Name.ToLower().Contains(searchLower)) ||
                (i.Customer != null && i.Customer.AppUser != null &&
                    (i.Customer.AppUser.FirstName.ToLower().Contains(searchLower) ||
                     i.Customer.AppUser.LastName.ToLower().Contains(searchLower))) ||
                i.BookingId.ToString().Contains(searchLower));
        }

        // Payment status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(i => i.PaymentStatus == status);
        }

        // Payment method filter
        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            query = query.Where(i => i.PaymentMethod == paymentMethod);
        }

        // Amount range filters
        if (minAmount.HasValue)
        {
            query = query.Where(i => i.TotalAmountWithTax >= minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            query = query.Where(i => i.TotalAmountWithTax <= maxAmount.Value);
        }

        // Date range filters
        if (startDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate <= endDate.Value);
        }

        // Cancelled filter
        if (isCancelled.HasValue)
        {
            query = query.Where(i => i.IsCancelled == isCancelled.Value);
        }

        // Get total count before pagination (single COUNT query)
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = sortBy?.ToLower() switch
        {
            "invoicenumber" => sortDescending ? query.OrderByDescending(i => i.InvoiceNumber) : query.OrderBy(i => i.InvoiceNumber),
            "totalamount" => sortDescending ? query.OrderByDescending(i => i.TotalAmountWithTax) : query.OrderBy(i => i.TotalAmountWithTax),
            "paymentstatus" => sortDescending ? query.OrderByDescending(i => i.PaymentStatus) : query.OrderBy(i => i.PaymentStatus),
            "customername" => sortDescending ? query.OrderByDescending(i => i.BuyerName) : query.OrderBy(i => i.BuyerName),
            "hallname" => sortDescending ? query.OrderByDescending(i => i.Hall != null ? i.Hall.Name : "") : query.OrderBy(i => i.Hall != null ? i.Hall.Name : ""),
            "createdat" => sortDescending ? query.OrderByDescending(i => i.CreatedAt) : query.OrderBy(i => i.CreatedAt),
            _ => sortDescending ? query.OrderByDescending(i => i.InvoiceDate) : query.OrderBy(i => i.InvoiceDate)
        };

        // Apply pagination
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<InvoiceStatisticsRaw> GetInvoiceStatisticsAsync(IEnumerable<int>? hallIds)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .AsQueryable();

        // Role-scope filter
        if (hallIds != null)
        {
            var hallIdsList = hallIds.ToList();
            query = query.Where(i => i.HallId.HasValue && hallIdsList.Contains(i.HallId.Value));
        }

        // Single aggregation query using GroupBy with a constant key
        var stats = await query
            .GroupBy(i => 1)
            .Select(g => new InvoiceStatisticsRaw
            {
                TotalRevenue = g.Sum(i => i.TotalAmountWithTax),
                PaidAmount = g.Where(i => i.PaymentStatus == "Paid").Sum(i => i.TotalAmountWithTax),
                PendingAmount = g.Where(i => i.PaymentStatus == "Pending").Sum(i => i.TotalAmountWithTax),
                TaxCollected = g.Sum(i => i.TaxAmount),
                TotalInvoices = g.Count(),
                PaidInvoices = g.Count(i => i.PaymentStatus == "Paid"),
                PendingInvoices = g.Count(i => i.PaymentStatus == "Pending"),
                CancelledInvoices = g.Count(i => i.IsCancelled)
            })
            .FirstOrDefaultAsync();

        return stats ?? new InvoiceStatisticsRaw();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<int>> GetHallIdsByOrganizationIdAsync(int organizationId)
    {
        return await _context.Halls
            .AsNoTracking()
            .Where(h => h.OrganizationId == organizationId)
            .Select(h => h.ID)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Invoice>> GetInvoicesByIdsAsync(IEnumerable<int> invoiceIds)
    {
        var idsList = invoiceIds.ToList();

        return await _context.Invoices
            .Include(i => i.Customer)
                .ThenInclude(c => c!.AppUser)
            .Include(i => i.Hall)
            .Include(i => i.LineItems)
            .AsNoTracking()
            .Where(i => idsList.Contains(i.Id))
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }
}
