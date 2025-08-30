using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.ReviewEntities;

namespace HallApp.Core.Entities.CustomerEntities;

public class Customer 
{
    public int Id { get; set; }
    public int NumberOfOrders { get; set; }
    public int SelectedAddressId { get; set; }
    public List<Address> Addresses { get; set; } = new List<Address>();
    public List<Review> Reviews { get; set; } = new List<Review>();

    public List<Booking> Bookings { get; set; } = new List<Booking>();
    public List<Favorite> Favorites { get; set; } = new List<Favorite>();
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
    public int CreditMoney { get; set; }
    
    // Additional properties to match original
    public bool Confirmed { get; set; }
    public bool Active { get; set; } = true;
}
