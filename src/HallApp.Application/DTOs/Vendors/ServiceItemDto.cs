using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Vendors;

public class ServiceItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int VendorId { get; set; }
}

public class CreateServiceItemDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string ServiceType { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Discounted price must be greater than 0")]
    public decimal? DiscountedPrice { get; set; }
    
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public int SortOrder { get; set; }
    
    [Required]
    public int VendorId { get; set; }
}

public class UpdateServiceItemDto
{
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string ServiceType { get; set; } = string.Empty;
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal? Price { get; set; }
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Discounted price must be greater than 0")]
    public decimal? DiscountedPrice { get; set; }
    
    public string ImageUrl { get; set; } = string.Empty;
    public bool? IsAvailable { get; set; }
    public int? SortOrder { get; set; }
}
