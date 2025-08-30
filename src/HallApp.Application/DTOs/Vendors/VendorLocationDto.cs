using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Vendors;

public class VendorLocationDto
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public class CreateVendorLocationDto
{
    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string City { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string State { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string Country { get; set; } = string.Empty;
    
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsPrimary { get; set; }
}

public class UpdateVendorLocationDto
{
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string City { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string State { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string Country { get; set; } = string.Empty;
    
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool? IsPrimary { get; set; }
    public bool? IsActive { get; set; }
}
