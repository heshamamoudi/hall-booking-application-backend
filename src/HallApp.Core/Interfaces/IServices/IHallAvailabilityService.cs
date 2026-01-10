namespace HallApp.Core.Interfaces.IServices;

public interface IHallAvailabilityService
{
    Task<bool> IsAvailableAsync(int hallId, DateTime eventDate, TimeSpan startTime, TimeSpan endTime);
    Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(int hallId, DateTime date);
    Task<List<DateTime>> GetAvailableDatesAsync(int hallId, DateTime startDate, DateTime endDate);
    Task<List<int>> GetAvailableHallsAsync(List<int> hallIds, DateTime eventDate, TimeSpan startTime, TimeSpan endTime);
    Task<(bool IsValid, string ErrorMessage)> ValidateBookingTimeAsync(int hallId, DateTime eventDate, TimeSpan startTime, TimeSpan endTime);
}

/// <summary>
/// Represents a time slot with availability status
/// </summary>
public class TimeSlot
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }

    public string StartTimeFormatted => StartTime.ToString(@"hh\:mm");
    public string EndTimeFormatted => EndTime.ToString(@"hh\:mm");
    public double DurationHours => (EndTime - StartTime).TotalHours;
}
