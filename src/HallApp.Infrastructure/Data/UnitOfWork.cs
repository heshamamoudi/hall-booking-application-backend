using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Data.Repositories;

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

    public async Task<int> Complete()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
