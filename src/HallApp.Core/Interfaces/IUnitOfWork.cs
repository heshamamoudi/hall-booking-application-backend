using HallApp.Core.Interfaces.IRepositories;

namespace HallApp.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IVendorRepository VendorRepository { get; }
    IServiceItemRepository ServiceItemRepository { get; }
    IVendorAvailabilityRepository VendorAvailabilityRepository { get; }
    IVendorManagerRepository VendorManagerRepository { get; }
    IVendorBookingRepository VendorBookingRepository { get; }
    IBookingRepository BookingRepository { get; }
    ICustomerRepository CustomerRepository { get; }
    IAddressRepository AddressRepository { get; }
    IHallRepository HallRepository { get; }
    IHallManagerRepository HallManagerRepository { get; }
    IReviewRepository ReviewRepository { get; }
    IFavoriteRepository FavoriteRepository { get; }
    IUserRepository UserRepository { get; }
    INotificationRepository NotificationRepository { get; }
    IChatRepository ChatRepository { get; }
    Task<int> Complete();
}
