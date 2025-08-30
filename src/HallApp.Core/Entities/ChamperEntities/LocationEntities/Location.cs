namespace HallApp.Core.Entities.ChamperEntities.LocationEntities;

public class Location
{
    public int ID { get; set; }
    public double Altitude { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int HallID { get; set; }
    public Hall Hall { get; set; } = null!;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}
