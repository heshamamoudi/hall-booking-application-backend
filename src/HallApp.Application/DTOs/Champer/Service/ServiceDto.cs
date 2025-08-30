using HallApp.Application.DTOs.Champer.Hall;

namespace HallApp.Application.DTOs.Champer.Service;

public class ServiceDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string ArabicName { get; set; }
    public bool IsActive { get; set; }
    public int Gender { get; set; }

    public int HallID { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}
