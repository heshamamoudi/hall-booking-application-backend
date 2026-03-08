namespace HallApp.Core.Entities.ChamperEntities.ServiceEntities;

public class Service
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Gender { get; set; }

    public int HallID { get; set; }
    public Hall Hall { get; set; } = null!;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}
