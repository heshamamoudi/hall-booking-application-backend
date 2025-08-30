namespace HallApp.Core.Entities.ChamperEntities.PackageEntities;

public class Package
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(1);
    public bool IsActive { get; set; }
    public int HallID { get; set; }
    public Hall Hall { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}
