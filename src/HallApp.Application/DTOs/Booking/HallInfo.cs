namespace HallApp.Application.DTOs.Booking;

public class HallInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class BookingServiceItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Quantity { get; set; }
    public string SpecialInstructions { get; set; } = string.Empty;
}
