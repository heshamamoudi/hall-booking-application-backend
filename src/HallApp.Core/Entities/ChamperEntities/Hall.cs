using HallApp.Core.Entities.ChamperEntities.ContactEntities;
using HallApp.Core.Entities.ChamperEntities.LocationEntities;
using HallApp.Core.Entities.ChamperEntities.MediaEntities;
using HallApp.Core.Entities.ChamperEntities.PackageEntities;
using HallApp.Core.Entities.ChamperEntities.ServiceEntities;
using HallApp.Core.Entities.ReviewEntities;

namespace HallApp.Core.Entities.ChamperEntities;

public class Hall
{
    public int ID { get; set; }
    public string Name { get; set; }

    public long CommercialRegisteration { get; set; }

    public long Vat { get; set; }
    // Properties for both
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

    // Direct contact information for independence
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;

    public string Logo { get; set; }
    public List<HallMedia> MediaFiles { get; set; } = new List<HallMedia>();
    public string Description { get; set; }
    public List<HallManager> Managers { get; set; }
    public List<Contact> Contacts { get; set; }
    public List<Review> Reviews { get; set; }
    public double AverageRating { get; set; }
    public Location Location { get; set; }
    public List<Package> Packages { get; set; }
    public List<Service> Services { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    public bool Active { get; set; } = true;
    
    // Section flags for categorization
    public bool HasSpecialOffer { get; set; } = false;
    public bool IsFeatured { get; set; } = false;
    public bool IsPremium { get; set; } = false;
}
