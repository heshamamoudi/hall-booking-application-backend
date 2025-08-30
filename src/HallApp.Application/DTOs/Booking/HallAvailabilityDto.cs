namespace HallApp.Application.DTOs.Booking
{
    public class HallAvailabilityDto
    {
        public int HallId { get; set; }
        public string HallName { get; set; }
        public DateTime Date { get; set; }
        public bool IsAvailable { get; set; }
        public List<TimeSlot> AvailableSlots { get; set; } = new List<TimeSlot>();
        public List<TimeSlotDto> BookedTimeSlots { get; set; } = new List<TimeSlotDto>();
    }

    public class TimeSlot
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBooked { get; set; }
    }
    
    public class TimeSlotDto
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int BookingId { get; set; }
    }
}
