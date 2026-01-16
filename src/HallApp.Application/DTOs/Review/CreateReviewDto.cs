using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Review;

public class CreateReviewDto
{
    [Required(ErrorMessage = "Rating is required")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }
    
    [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
    public string Comment { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Customer ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Valid customer ID is required")]
    public int CustomerId { get; set; }
    
    public int? HallId { get; set; }
    public int? VendorId { get; set; }
}
