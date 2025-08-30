namespace HallApp.Application.DTOs.Booking
{
    public class BookingStatisticsDto
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
    }
}
