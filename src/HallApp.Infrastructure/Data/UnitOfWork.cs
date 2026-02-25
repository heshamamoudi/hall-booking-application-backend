using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace HallApp.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly DataContext _context;

    public UnitOfWork(DataContext context)
    {
        _context = context;
        VendorRepository = new VendorRepository(_context);
        ServiceItemRepository = new ServiceItemRepository(_context);
        VendorAvailabilityRepository = new VendorAvailabilityRepository(_context);
        VendorManagerRepository = new VendorManagerRepository(_context);
        VendorBookingRepository = new VendorBookingRepository(_context);
        BookingRepository = new BookingRepository(_context);
        CustomerRepository = new CustomerRepository(_context);
        AddressRepository = new AddressRepository(_context);
        HallRepository = new HallRepository(_context);
        HallManagerRepository = new HallManagerRepository(_context);
        ReviewRepository = new ReviewRepository(_context);
        FavoriteRepository = new FavoriteRepository(_context);
        NotificationRepository = new NotificationRepository(_context);
        ChatRepository = new ChatRepository(_context);
        InvoiceRepository = new InvoiceRepository(_context);
        PaymentRepository = new PaymentRepository(_context);
        PaymentRefundRepository = new PaymentRefundRepository(_context);
        HallBlockedDateRepository = new HallBlockedDateRepository(_context);
        IdempotencyKeyRepository = new IdempotencyKeyRepository(_context);
        FinancialAuditLogRepository = new FinancialAuditLogRepository(_context);
        DataRetentionRequestRepository = new DataRetentionRequestRepository(_context);
        // UserRepository requires additional dependencies - should be injected via DI
    }

    public IVendorRepository VendorRepository { get; private set; }
    public IServiceItemRepository ServiceItemRepository { get; private set; }
    public IVendorAvailabilityRepository VendorAvailabilityRepository { get; private set; }
    public IVendorManagerRepository VendorManagerRepository { get; private set; }
    public IVendorBookingRepository VendorBookingRepository { get; private set; }
    public IBookingRepository BookingRepository { get; private set; }
    public ICustomerRepository CustomerRepository { get; private set; }
    public IAddressRepository AddressRepository { get; private set; }
    public IHallRepository HallRepository { get; private set; }
    public IHallManagerRepository HallManagerRepository { get; private set; }
    public IReviewRepository ReviewRepository { get; private set; }
    public IFavoriteRepository FavoriteRepository { get; private set; }
    public IUserRepository UserRepository { get; private set; }
    public INotificationRepository NotificationRepository { get; private set; }
    public IChatRepository ChatRepository { get; private set; }
    public IInvoiceRepository InvoiceRepository { get; private set; }
    public IPaymentRepository PaymentRepository { get; private set; }
    public IPaymentRefundRepository PaymentRefundRepository { get; private set; }
    public IHallBlockedDateRepository HallBlockedDateRepository { get; private set; }
    public IIdempotencyKeyRepository IdempotencyKeyRepository { get; private set; }
    public IFinancialAuditLogRepository FinancialAuditLogRepository { get; private set; }
    public IDataRetentionRequestRepository DataRetentionRequestRepository { get; private set; }

    public async Task<int> Complete()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        // For PostgreSQL, set the transaction isolation level using raw SQL before beginning transaction
        // This is the recommended approach for EF Core 8+
        if (isolationLevel != IsolationLevel.ReadCommitted)
        {
            var isolationLevelName = isolationLevel switch
            {
                IsolationLevel.Serializable => "SERIALIZABLE",
                IsolationLevel.RepeatableRead => "REPEATABLE READ",
                IsolationLevel.ReadUncommitted => "READ UNCOMMITTED",
                _ => "READ COMMITTED"
            };

            await _context.Database.ExecuteSqlRawAsync($"SET TRANSACTION ISOLATION LEVEL {isolationLevelName}");
        }

        return await _context.Database.BeginTransactionAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
