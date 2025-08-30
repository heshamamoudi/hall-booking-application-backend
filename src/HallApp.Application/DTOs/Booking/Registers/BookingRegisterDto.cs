using HallApp.Application.DTOs.Booking.Registers;
using HallApp.Core.Entities.BookingEntities;

namespace HallApp.Application.DTOs.Booking.Registers;

public class BookingRegisterDto
{
    public int HallId { get; set; }
    public int CustomerId { get; set; }
    public string PaymentMethod { get; set; }
    public string Coupon { get; set; }
    public string Status { get; set; }
    public double Tax { get; set; }
    public double TotalPrice { get; set; }
    public string Comments { get; set; }
    public double Discount { get; set; }
    public DateTime VisitDate { get; set; }
    public bool IsVisitCompleted { get; set; }
    public bool IsBookingConfirmed { get; set; }
    public DateTime BookingDate { get; set; }
    public BookingPackageRegisterDto PackageDetails { get; set; }
}
