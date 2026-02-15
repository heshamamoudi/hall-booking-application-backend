namespace HallApp.Application.DTOs.Review;

/// <summary>
/// DTO for a hall manager's response to a customer review.
/// </summary>
public class ReviewResponseRequestDto
{
    /// <summary>
    /// The manager's response text (1-1000 characters).
    /// </summary>
    public string Response { get; set; } = string.Empty;
}
