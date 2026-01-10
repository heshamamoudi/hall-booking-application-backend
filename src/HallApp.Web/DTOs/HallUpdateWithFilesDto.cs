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
        
        public string CommercialRegisteration { get; set; }
        public string Vat { get; set; }
        public int BothWeekDays { get; set; }
        public int BothWeekEnds { get; set; }
        public int MaleWeekDays { get; set; }
        public int MaleWeekEnds { get; set; }
        public int MaleMin { get; set; }
        public int MaleMax { get; set; }
        public int FemaleWeekDays { get; set; }
        public int FemaleWeekEnds { get; set; }
        public int FemaleMin { get; set; }
        public int FemaleMax { get; set; }
        public bool FemaleActive { get; set; }
        public int Gender { get; set; }
        public string Description { get; set; }
        
        // List of existing image URLs to keep (from database)
        public List<string> ExistingImageUrls { get; set; } = new List<string>();
        
        // New image files being uploaded
        public List<IFormFile> NewImages { get; set; } = new List<IFormFile>();
    }
}
