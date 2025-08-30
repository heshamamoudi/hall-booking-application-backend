using HallApp.Core.Entities.ReviewEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IReviewRepository : IGenericRepository<Review>
{
    Task<IEnumerable<Review>> GetReviewsByHallIdAsync(int hallId);
    Task<IEnumerable<Review>> GetReviewsByCustomerIdAsync(int customerId);
    Task<Review> GetReviewWithDetailsAsync(int reviewId);
    Task<double> GetAverageRatingByHallIdAsync(int hallId);
    Task<IEnumerable<Review>> GetRecentReviewsAsync(int count = 10);
    Task<bool> CustomerHasReviewedHallAsync(int customerId, int hallId);
}
