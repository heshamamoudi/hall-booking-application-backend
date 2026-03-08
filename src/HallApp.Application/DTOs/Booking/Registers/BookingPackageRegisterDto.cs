namespace HallApp.Application.DTOs.Booking.Registers;

public class BookingPackageRegisterDto
{
    public int Id { get; set; }  // This represents the package ID

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public int BookingId { get; set; }
}
