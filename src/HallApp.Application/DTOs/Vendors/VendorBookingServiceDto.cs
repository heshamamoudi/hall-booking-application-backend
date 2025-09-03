namespace HallApp.Application.DTOs.Vendors;

public class VendorBookingServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Quantity { get; set; }
    public string SpecialInstructions { get; set; } = string.Empty;
    public int VendorBookingId { get; set; }
}
