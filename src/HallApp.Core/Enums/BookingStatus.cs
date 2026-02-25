namespace HallApp.Core.Enums;

/// <summary>
/// Booking workflow status enum matching customer app
/// </summary>
public enum BookingStatus
{
    /// <summary>Initial creation, not yet submitted</summary>
    Draft = 0,
    
    /// <summary>Submitted, waiting for hall approval</summary>
    Pending = 1,
    
    /// <summary>Hall approved, waiting for vendor approvals</summary>
    HallApproved = 2,
    
    /// <summary>Vendors are reviewing in parallel</summary>
    VendorsApproving = 3,
    
    /// <summary>All vendors approved, or partial approval (some approved, some rejected) - ready for payment with approved vendors</summary>
    ReadyForPayment = 4,
    
    /// <summary>Payment completed</summary>
    Paid = 5,
    
    /// <summary>Final confirmed state</summary>
    Confirmed = 6,
    
    /// <summary>Booking cancelled by customer or admin</summary>
    Cancelled = 7,
    
    /// <summary>Hall rejected the booking</summary>
    HallRejected = 8,
    
    /// <summary>ALL vendors rejected the booking. Partial rejections (some approved, some rejected) proceed to ReadyForPayment instead.</summary>
    VendorRejected = 9
}

/// <summary>
/// Approval status for hall and vendor approvals
/// </summary>
public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// DUP-005 FIX: Centralized booking status string constants.
/// Use these instead of magic strings throughout the codebase.
/// Maps to the BookingStatus enum names for consistency.
/// </summary>
public static class BookingStatusConstants
{
    public const string Draft = nameof(BookingStatus.Draft);
    public const string Pending = nameof(BookingStatus.Pending);
    public const string HallApproved = nameof(BookingStatus.HallApproved);
    public const string VendorsApproving = nameof(BookingStatus.VendorsApproving);
    public const string ReadyForPayment = nameof(BookingStatus.ReadyForPayment);
    public const string Paid = nameof(BookingStatus.Paid);
    public const string Confirmed = nameof(BookingStatus.Confirmed);
    public const string Cancelled = nameof(BookingStatus.Cancelled);
    public const string HallRejected = nameof(BookingStatus.HallRejected);
    public const string VendorRejected = nameof(BookingStatus.VendorRejected);
    public const string Completed = "Completed";

    /// <summary>Statuses considered "pending" for dashboard counts</summary>
    public static readonly string[] PendingStatuses = [Pending, Draft];

    /// <summary>Statuses considered "active" for dashboard counts</summary>
    public static readonly string[] ActiveStatuses = [HallApproved, VendorsApproving, ReadyForPayment, Paid, Confirmed];

    /// <summary>Statuses considered "completed" for dashboard counts</summary>
    public static readonly string[] CompletedStatuses = [Completed];

    /// <summary>Statuses considered "cancelled" for dashboard counts</summary>
    public static readonly string[] CancelledStatuses = [Cancelled, HallRejected, VendorRejected];

    /// <summary>Statuses where payment has been made</summary>
    public static readonly string[] PaidStatuses = [Paid, Confirmed, Completed];
}
