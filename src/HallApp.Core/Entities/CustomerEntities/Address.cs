using System.ComponentModel.DataAnnotations;

namespace HallApp.Core.Entities.CustomerEntities;

public class Address
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Street { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string State { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string ZipCode { get; set; } = string.Empty;
    
    public bool IsMain { get; set; } = false;
    
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
}
