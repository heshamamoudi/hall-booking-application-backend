namespace HallApp.Application.DTOs.Review;

/// <summary>
/// DTO for flagging an inappropriate review.
/// </summary>
public class FlagReviewRequestDto
{
    /// <summary>
    /// The reason for flagging the review (1-500 characters).
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
