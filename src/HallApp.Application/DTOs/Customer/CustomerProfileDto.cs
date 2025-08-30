namespace HallApp.Application.DTOs.Customer;

/// <summary>
/// Complete customer profile combining AppUser + Customer data
/// Used for profile endpoints where full user context is needed
/// </summary>
public class CustomerProfileDto
{
    // From AppUser (Identity)
    public int AppUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool Active { get; set; }
    public DateTime UserCreated { get; set; }
    
    // From Customer (Business Domain)
    public int CustomerId { get; set; }
    public int CreditMoney { get; set; }
    public int NumberOfOrders { get; set; }
    public int SelectedAddressId { get; set; }
    public bool CustomerConfirmed { get; set; }
    public DateTime CustomerCreated { get; set; }
    public DateTime CustomerUpdated { get; set; }
    
    // Business Relationships
    public List<AddressDto> Addresses { get; set; } = new();
    public List<FavoriteDto> Favorites { get; set; } = new();
    public int TotalBookings { get; set; }
    public int TotalReviews { get; set; }
}
