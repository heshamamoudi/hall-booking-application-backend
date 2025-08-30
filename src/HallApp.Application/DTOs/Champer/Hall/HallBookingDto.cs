using HallApp.Application.DTOs.Champer.HallManager;
using HallApp.Application.DTOs.Champer.Media;

namespace HallApp.Application.DTOs.Champer.Hall;

public class HallBookingDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    public List<HallManagerDto> Managers { get; set; }
}
