namespace HallApp.Application.DTOs.Booking
{
    public class BookingRequestDto
    {
        public int HallId { get; set; }
        public int CustomerId { get; set; }
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int GenderPreference { get; set; } // 0=Male, 1=Female, 2=Both
        public string EventType { get; set; } = string.Empty;
        public string SpecialRequests { get; set; } = string.Empty;
        public int ExpectedGuestCount { get; set; }
        public List<VendorServiceSelectionDto> SelectedServices { get; set; } = new();
    }

    public class VendorServiceSelectionDto
    {
        public int VendorId { get; set; }
        public int ServiceItemId { get; set; }
        public int Quantity { get; set; } = 1;
        public string SpecialInstructions { get; set; } = string.Empty;
    }

    public class BookingResponseDto
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public BookingStatus Status { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime CreatedAt { get; set; }
        public string HallName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public List<BookingServiceDto> Services { get; set; } = new();
        public List<BookingApprovalDto> ApprovalStatus { get; set; } = new();
    }

    public class BookingServiceDto
    {
        public string VendorName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class BookingApprovalDto
    {
        public string ApproverType { get; set; } = string.Empty; // "Hall", "Vendor"
        public string ApproverName { get; set; } = string.Empty;
        public BookingStatus Status { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public enum BookingStatus
    {
        Pending = 0,
        HallApproved = 1,
        VendorApprovalInProgress = 2,
        Confirmed = 3,
        Rejected = 4,
        Cancelled = 5
    }
}
