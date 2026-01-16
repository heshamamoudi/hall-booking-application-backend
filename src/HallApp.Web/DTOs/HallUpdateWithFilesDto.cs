using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace HallApp.Web.DTOs
{
    public class HallUpdateWithFilesDto
    {
        [Required]
        public int ID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public string CommercialRegistration { get; set; }
        public string Vat { get; set; }
        public int BothWeekDays { get; set; }
        public int BothWeekEnds { get; set; }
        public int MaleWeekDays { get; set; }
        public int MaleWeekEnds { get; set; }
        public int MaleMin { get; set; }
        public int MaleMax { get; set; }
        public bool MaleActive { get; set; }
        public int FemaleWeekDays { get; set; }
        public int FemaleWeekEnds { get; set; }
        public int FemaleMin { get; set; }
        public int FemaleMax { get; set; }
        public bool FemaleActive { get; set; }
        public int Gender { get; set; }
        public string Description { get; set; }
        
        // Direct contact information
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string WhatsApp { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
        
        // Flags
        public bool Active { get; set; } = true;
        public bool HasSpecialOffer { get; set; } = false;
        public bool IsFeatured { get; set; } = false;
        public bool IsPremium { get; set; } = false;
        
        // Manager IDs to assign (list of HallManager IDs)
        public List<int> ManagerIds { get; set; } = new List<int>();
        
        // Contacts as JSON string (for form data)
        public string ContactsJson { get; set; }
        
        // Location as JSON string (for form data)
        public string LocationJson { get; set; }
        
        // Packages as JSON string (for form data)
        public string PackagesJson { get; set; }
        
        // Services as JSON string (for form data)
        public string ServicesJson { get; set; }
        
        // List of existing image URLs to keep (from database)
        public List<string> ExistingImageUrls { get; set; } = new List<string>();
        
        // New image files being uploaded
        public List<IFormFile> NewImages { get; set; } = new List<IFormFile>();
    }
}
