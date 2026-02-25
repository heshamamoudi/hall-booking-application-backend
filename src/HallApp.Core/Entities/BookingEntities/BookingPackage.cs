using System.ComponentModel.DataAnnotations.Schema;

namespace HallApp.Core.Entities.BookingEntities;

public class BookingPackage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // CRIT-003 FIX: Changed from double to decimal for financial precision
    // HIGH-005 FIX: Added explicit decimal precision
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    public int MaxGuests { get; set; }
    public string Features { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
}
