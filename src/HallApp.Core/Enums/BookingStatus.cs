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
    
    /// <summary>All vendors approved OR all rejected - ready for payment</summary>
    ReadyForPayment = 4,
    
    /// <summary>Payment completed</summary>
    Paid = 5,
    
    /// <summary>Final confirmed state</summary>
    Confirmed = 6,
    
    /// <summary>Booking cancelled by customer or admin</summary>
    Cancelled = 7,
    
    /// <summary>Hall rejected the booking</summary>
    HallRejected = 8,
    
    /// <summary>At least one vendor rejected</summary>
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
