using HallApp.Core.Entities.ReviewEntities;

namespace HallApp.Core.Interfaces.IServices
{
    public interface IReviewService
    {
        Task<Review?> CreateReviewAsync(Review review);
        Task<Review?> GetReviewByIdAsync(int reviewId);
        Task<IEnumerable<Review>> GetAllReviewsAsync();
        Task<IEnumerable<Review>> GetReviewsByHallIdAsync(int hallId);
        Task<IEnumerable<Review>> GetReviewsByCustomerIdAsync(string customerId);
        Task<IEnumerable<Review>> GetReviewsByUserIdAsync(string userId);
        Task<Review?> UpdateReviewAsync(Review review);
        Task<bool> DeleteReviewAsync(int reviewId);
        Task<IEnumerable<Review>> GetPendingReviewsAsync();
        Task<bool> ApproveReviewAsync(int reviewId);
        Task<bool> RejectReviewAsync(int reviewId, string reason);
        Task<object> GetReviewStatisticsAsync();
        Task<Review?> GetReviewEntityById(int reviewId);
        Task DeleteReview(Review review);
        Task<Review?> UpdateUserReviewAsync(string userId, Review review);
        Task<bool> ToggleReviewApprovalAsync(int reviewId);
        Task<bool> FlagReviewAsync(int reviewId);
        Task<IEnumerable<Review>> GetFlaggedReviewsAsync();
        Task<bool> HasConfirmedBooking(int hallId, string customerId);
        Task<Review?> GetReviewByCustomerAndHall(string customerId, int hallId);
        Task<IEnumerable<Review>> GetReviewsByRatingAsync(int rating);
        Task<double> GetAverageRatingForHallAsync(int hallId);
        Task<IEnumerable<Review>> GetRecentReviewsAsync(int count);
        Task<IEnumerable<Review>> SearchReviewsAsync(string searchTerm);
        Task<bool> ValidateReviewPermissionsAsync(int reviewId, string userId);
        Task<int> GetReviewCountForHallAsync(int hallId);
    }
}
