namespace HallApp.Application.DTOs.Review;

/// <summary>
/// DTO for displaying a review from the hall manager's perspective.
/// Includes customer details, booking reference, and manager response.
/// </summary>
public class ManagerReviewDto
{
    public int Id { get; set; }
    public int HallId { get; set; }
    public string HallName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public bool IsApproved { get; set; }
    public bool IsFlagged { get; set; }

    // Customer info
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    // Manager response
    public string? ManagerResponse { get; set; }
    public DateTime? ManagerResponseDate { get; set; }
    public string? ManagerResponderName { get; set; }
    public bool HasResponse => !string.IsNullOrEmpty(ManagerResponse);

    // Flag info
    public string? FlagReason { get; set; }
    public DateTime? FlaggedDate { get; set; }
}
