using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Booking.Registers;

public class BookingRegisterDto
{
    [Required(ErrorMessage = "Hall ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Valid hall ID is required")]
    public int HallId { get; set; }
    
    [Required(ErrorMessage = "Customer ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Valid customer ID is required")]
    public int CustomerId { get; set; }
    
    [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
    public string PaymentMethod { get; set; }
    
    [StringLength(50, ErrorMessage = "Coupon code cannot exceed 50 characters")]
    public string Coupon { get; set; }
    
    [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
    public string Status { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Tax must be a positive value")]
    public double Tax { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Total price must be a positive value")]
    public double TotalPrice { get; set; }
    
    [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters")]
    public string Comments { get; set; }
    
    [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
    public double Discount { get; set; }
    
    public DateTime VisitDate { get; set; }
    public bool IsVisitCompleted { get; set; }
    public bool IsBookingConfirmed { get; set; }
    
    [Required(ErrorMessage = "Booking date is required")]
    public DateTime BookingDate { get; set; }
    
    public BookingPackageRegisterDto PackageDetails { get; set; }
}
