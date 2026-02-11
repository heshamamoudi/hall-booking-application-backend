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

        /// <summary>
        /// Saudi Commercial Registration number (9 or 10 digits)
        /// </summary>
        [Range(100000000, 9999999999, ErrorMessage = "Commercial Registration must be 9 or 10 digits.")]
        public long CommercialRegistration { get; set; }

        /// <summary>
        /// Saudi VAT Registration number (up to 15 digits)
        /// </summary>
        [Range(0, 999999999999999, ErrorMessage = "VAT number must not exceed 15 digits.")]
        public long Vat { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Both weekday price must be 0 or greater.")]
        public double BothWeekDays { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Both weekend price must be 0 or greater.")]
        public double BothWeekEnds { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Male weekday price must be 0 or greater.")]
        public double MaleWeekDays { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Male weekend price must be 0 or greater.")]
        public double MaleWeekEnds { get; set; }

        public int MaleMin { get; set; }
        public int MaleMax { get; set; }
        public bool MaleActive { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Female weekday price must be 0 or greater.")]
        public double FemaleWeekDays { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Female weekend price must be 0 or greater.")]
        public double FemaleWeekEnds { get; set; }

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
