namespace HallApp.Application.DTOs.Booking.Updaters;

public class BookingPackageUpdateDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime Created { get; set; } 
    public DateTime Updated { get; set; } 
    public int BookingId { get; set; }
}
