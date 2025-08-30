namespace HallApp.Application.DTOs.Customer.Registers;

public class RegisterCustomerDto 
{
    public int NumberOfOrders { get; set; }
    public int SelectedAddressId { get; set; }
    public List<RegisterAddressDto> Addresses { get; set; }
    public List<RegisterFavoriteDto> Favorites { get; set; }
    public int AppUserId { get; set; }
}
