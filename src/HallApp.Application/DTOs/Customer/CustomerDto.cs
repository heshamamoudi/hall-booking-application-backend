using HallApp.Application.DTOs.Booking;
using HallApp.Application.DTOs.Customer;

namespace HallApp.Application.DTOs.Customer;

public class CustomerDto
{
    public int Id { get; set; }
    public int CreditMoney { get; set; }
    public int NumberOfOrders { get; set; }
    public int SelectedAddressId { get; set; }
    public List<BookingDto> Bookings { get; set; }  = new List<BookingDto>();
    public List<AddressDto> Addresses { get; set; } = new List<AddressDto>();
    public List<FavoriteDto> Favorites { get; set; } = new List<FavoriteDto>();
    public DateTime Created { get; set; } 
    public DateTime Updated { get; set; }
    public int AppUserId { get; set; }
}
