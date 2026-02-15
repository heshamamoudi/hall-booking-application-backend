namespace HallApp.Application.DTOs.Review;

/// <summary>
/// DTO for review statistics from the hall manager's perspective.
/// Shows aggregate metrics across the manager's hall(s).
/// </summary>
public class ManagerReviewStatsDto
{
    public int HallId { get; set; }
    public string HallName { get; set; } = string.Empty;
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; }
    public int RespondedCount { get; set; }
    public int NotRespondedCount { get; set; }
    public int FlaggedCount { get; set; }

    /// <summary>
    /// Rating distribution: key = star rating (1-5), value = count
    /// </summary>
    public Dictionary<int, int> RatingDistribution { get; set; } = new()
    {
        { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
    };
}
