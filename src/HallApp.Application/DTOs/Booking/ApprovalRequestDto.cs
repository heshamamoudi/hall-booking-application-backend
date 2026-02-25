#nullable enable

namespace HallApp.Application.DTOs.Booking;

public class HallApprovalRequestDto
{
    public bool Approved { get; set; }
    public string? RejectionReason { get; set; }
}

public class VendorApprovalRequestDto
{
    public int VendorBookingId { get; set; }
    public bool Approved { get; set; }
    public string? RejectionReason { get; set; }
}

public class ApprovalResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public bool CanProceedToPayment { get; set; }
}

public class VendorApprovalStatusDto
{
    public int TotalVendors { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int PendingCount { get; set; }
    public bool AllApproved { get; set; }
    public bool CanProceedToPayment { get; set; }

    /// <summary>
    /// True when some vendors approved and some rejected (not all-approved, not all-rejected).
    /// The customer can proceed with approved vendors or replace rejected ones.
    /// </summary>
    public bool IsPartialApproval { get; set; }

    /// <summary>
    /// Names of vendors that rejected. Empty when no rejections.
    /// </summary>
    public List<string> RejectedVendorNames { get; set; } = [];
}
