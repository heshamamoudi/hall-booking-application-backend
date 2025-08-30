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
    public decimal BothWeekDays { get; set; }
    public decimal BothWeekEnds { get; set; }

    // Properties for Male
    public decimal MaleWeekDays { get; set; }
    public decimal MaleWeekEnds { get; set; }
    public int MaleMin { get; set; }
    public int MaleMax { get; set; }
    public bool MaleActive { get; set; } = false;

    // Properties for Female
    public decimal FemaleWeekDays { get; set; }
    public decimal FemaleWeekEnds { get; set; }
    public int FemaleMin { get; set; }
    public int FemaleMax { get; set; }
    public bool FemaleActive { get; set; } = false;
    // Gender 1 = Male , 2 = Female , 3 = Both
    public int Gender { get; set; }

    public string Logo { get; set; }
    public List<HallMedia> MediaFiles { get; set; } = [];
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
}
