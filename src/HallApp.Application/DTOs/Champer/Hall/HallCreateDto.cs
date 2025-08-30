using System.ComponentModel.DataAnnotations;
using HallApp.Application.DTOs.Champer.HallManager;
using HallApp.Application.DTOs.Champer.Package;
using HallApp.Application.DTOs.Champer.Service;
using HallApp.Application.DTOs.Champer.Contact;
using HallApp.Application.DTOs.Champer.Location;
using HallApp.Application.DTOs.Champer.Media;

namespace HallApp.Application.DTOs.Champer.Hall;

public class HallCreateDto
{
    public string Name { get; set; }
     [Range(1000000000, 9999999999, ErrorMessage = "Commercial Registration must be exactly 10 digits.")]
    public long CommercialRegisteration { get; set; }

    [Range(100000000000000, 999999999999999, ErrorMessage = "VAT must be exactly 15 digits.")]
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
    public List<HallMediaDto> MediaFiles { get; set; }
    public List<UserCreateDto> Managers { get; set; }
    public List<ContactDto> Contacts { get; set; }
    public LocationDto Location { get; set; }
    public List<PackageDto> Packages { get; set; }
    public List<ServiceDto> Services { get; set; }
    public List<string> mediaBeforeUploade { get; set; }
}
