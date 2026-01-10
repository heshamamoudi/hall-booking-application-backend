using HallApp.Application.DTOs.Champer.HallManager;
using HallApp.Application.DTOs.Champer.Package;
using HallApp.Application.DTOs.Champer.Service;
using HallApp.Application.DTOs.Champer.Contact;
using HallApp.Application.DTOs.Champer.Location;
using HallApp.Application.DTOs.Champer.Media;

namespace HallApp.Application.DTOs.Champer.Hall;

public class HallDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    public long CommercialRegisteration { get; set; }

    public long Vat { get; set; }
    public double BothWeekDays { get; set; }
    public double BothWeekEnds { get; set; }

    // Properties for Male
    public double MaleWeekDays { get; set; }
    public double MaleWeekEnds { get; set; }
    public int MaleMin { get; set; }
    public int MaleMax { get; set; }
    public bool MaleActive { get; set; } = false;

    // Properties for Female
    public double FemaleWeekDays { get; set; }
    public double FemaleWeekEnds { get; set; }
    public int FemaleMin { get; set; }
    public int FemaleMax { get; set; }
    public bool FemaleActive { get; set; } = false;

    // Gender 1 = Male , 2 = Female , 3 = Both
    public int Gender { get; set; }

    public string Description { get; set; }
    public double AverageRating { get; set; }
    public string Media { get; set; }
    public List<HallMediaDto> MediaFiles { get; set; } = new List<HallMediaDto>();
    public List<HallManagerSimpleDto> Managers { get; set; } = new List<HallManagerSimpleDto>();
    public List<ContactDto> Contacts { get; set; } = new List<ContactDto>();
    public LocationDto Location { get; set; }
    public List<PackageDto> Packages { get; set; } = new List<PackageDto>();
    public List<ServiceDto> Services { get; set; } = new List<ServiceDto>();
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    
    // Approval workflow
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
