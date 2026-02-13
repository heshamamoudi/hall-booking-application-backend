using HallApp.Application.DTOs.Halls.HallManager;
using HallApp.Application.DTOs.Halls.Package;
using HallApp.Application.DTOs.Halls.Service;
using HallApp.Application.DTOs.Halls.Contact;
using HallApp.Application.DTOs.Halls.Location;
using HallApp.Application.DTOs.Halls.Media;

namespace HallApp.Application.DTOs.Halls.Hall;

/// <summary>
/// Extended hall DTO that includes basic statistics for list views.
/// Used by the enhanced GET /api/halls endpoint when includeStats=true.
/// Inherits all HallDto properties and adds aggregate stat fields.
/// </summary>
public class HallWithStatsDto
{
    // Core hall properties (mirroring HallDto)
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public long CommercialRegistration { get; set; }
    public long Vat { get; set; }
    public double BothWeekDays { get; set; }
    public double BothWeekEnds { get; set; }
    public double MaleWeekDays { get; set; }
    public double MaleWeekEnds { get; set; }
    public int MaleMin { get; set; }
    public int MaleMax { get; set; }
    public bool MaleActive { get; set; }
    public double FemaleWeekDays { get; set; }
    public double FemaleWeekEnds { get; set; }
    public int FemaleMin { get; set; }
    public int FemaleMax { get; set; }
    public bool FemaleActive { get; set; }
    public int Gender { get; set; }
    public string Description { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public string Media { get; set; } = string.Empty;
    public List<HallMediaDto> MediaFiles { get; set; } = [];
    public List<HallManagerSimpleDto> Managers { get; set; } = [];
    public List<ContactDto> Contacts { get; set; } = [];
    public LocationDto? Location { get; set; }
    public List<PackageDto> Packages { get; set; } = [];
    public List<ServiceDto> Services { get; set; } = [];
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool HasSpecialOffer { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPremium { get; set; }

    // Statistics (nullable so omitted from output when not requested)
    public int? TotalBookings { get; set; }
    public decimal? TotalRevenue { get; set; }
    public int? PaidInvoices { get; set; }
    public int? PendingInvoices { get; set; }
    public int? TotalReviews { get; set; }
}
