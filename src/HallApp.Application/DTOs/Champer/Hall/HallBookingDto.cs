using HallApp.Application.DTOs.Champer.HallManager;
using HallApp.Application.DTOs.Champer.Media;
using HallApp.Application.DTOs.Champer.Location;

namespace HallApp.Application.DTOs.Champer.Hall;

public class HallBookingDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public LocationDto Location { get; set; }
    public List<HallManagerDto> Managers { get; set; } = new();
}
