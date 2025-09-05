using HallApp.Application.DTOs.Champer.Hall;

namespace HallApp.Application.DTOs.Champer.Location;

public class LocationDto
{
    public int ID { get; set; }
    public double Altitude { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int HallID { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}
