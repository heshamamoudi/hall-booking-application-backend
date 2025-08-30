using HallApp.Application.DTOs.Booking;
using HallApp.Application.DTOs.Champer.Hall;
using HallApp.Application.DTOs.Customer;
using HallApp.Core.Entities.BookingEntities;

namespace HallApp.Application.DTOs.Booking;

public class BookingDto
{
    public int Id { get; set; }
    public int HallId { get; set; }

    public HallBookingDto Hall {  get; set; }
    public int CustomerId { get; set; }
    public BookingCustomerDto Customer { get; set; }
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
    public BookingPackage PackageDetails { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}
