namespace HallApp.Core.Entities.ChamperEntities.MediaEntities;

public class Media
{
    public int ID { get; set; }
    public string MediaType { get; set; } = string.Empty;  // e.g., image, video, etc.
    public string URL { get; set; } = string.Empty;
    public int HallID { get; set; }
    public Hall Hall { get; set; } = null!;
    public int index { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}
