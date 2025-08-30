using HallApp.Application.DTOs.Champer.Hall;

namespace HallApp.Application.DTOs.Champer.Package;

public class PackageDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public int HallID { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}
