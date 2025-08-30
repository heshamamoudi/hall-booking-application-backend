using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Entities.CustomerEntities;

public class Favorite
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int VendorId { get; set; }
    public int HallId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Customer Customer { get; set; } = null!;
    public Hall Hall { get; set; } = null!;
}
