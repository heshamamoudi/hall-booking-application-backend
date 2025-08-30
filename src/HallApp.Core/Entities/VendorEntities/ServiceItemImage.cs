namespace HallApp.Core.Entities.VendorEntities;

public class ServiceItemImage
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int ServiceItemId { get; set; }
    public ServiceItem ServiceItem { get; set; } = null!;
}
