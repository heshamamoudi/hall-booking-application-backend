using HallApp.Application.DTOs.Halls.HallManager;
using HallApp.Application.DTOs.Halls.Media;
using HallApp.Application.DTOs.Halls.Location;

namespace HallApp.Application.DTOs.Halls.Hall;

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
    public List<HallMediaDto> MediaFiles { get; set; } = new();
}
