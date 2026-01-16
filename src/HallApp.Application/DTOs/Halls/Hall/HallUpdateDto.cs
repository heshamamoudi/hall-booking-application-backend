using System.ComponentModel.DataAnnotations;
using HallApp.Application.DTOs.Halls.HallManager;
using HallApp.Application.DTOs.Halls.Package;
using HallApp.Application.DTOs.Halls.Service;
using HallApp.Application.DTOs.Halls.Contact;
using HallApp.Application.DTOs.Halls.Location;
using HallApp.Application.DTOs.Halls.Media;

namespace HallApp.Application.DTOs.Halls.Hall;

public class HallUpdateDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    [Range(1000000000, 9999999999, ErrorMessage = "Commercial Registration must be exactly 10 digits.")]
    public long CommercialRegistration { get; set; }

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

    // Direct contact information
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;

    // Related entities
    public List<HallMediaDto> MediaFiles { get; set; }
    public List<UserUpdateDto> Managers { get; set; }
    public List<ContactDto> Contacts { get; set; }
    public LocationDto Location { get; set; }
    public List<PackageDto> Packages { get; set; }
    public List<ServiceDto> Services { get; set; }
    public List<string> mediaBeforeUpload { get; set; }
    
    // Flags for categorization
    public bool IsActive { get; set; } = true;
    public bool IsApproved { get; set; } = false;
    public bool HasSpecialOffer { get; set; } = false;
    public bool IsFeatured { get; set; } = false;
    public bool IsPremium { get; set; } = false;
    
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}
