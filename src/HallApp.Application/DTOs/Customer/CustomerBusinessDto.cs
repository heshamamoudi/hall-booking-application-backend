namespace HallApp.Application.DTOs.Customer;

/// <summary>
/// Pure business domain DTO for Customer entity
/// Used for business operations (bookings, orders, etc.)
/// </summary>
public class CustomerBusinessDto
{
    public int Id { get; set; }
    public int CreditMoney { get; set; }
    public int NumberOfOrders { get; set; }
    public int SelectedAddressId { get; set; }
    public bool Confirmed { get; set; }
    public bool Active { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    
    // Business relationships (without AppUser mixing)
    public List<AddressDto> Addresses { get; set; } = new();
    public List<FavoriteDto> Favorites { get; set; } = new();
}
