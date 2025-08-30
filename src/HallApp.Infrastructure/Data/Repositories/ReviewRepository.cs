using HallApp.Core.Entities.ReviewEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    public ReviewRepository(DataContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Review>> GetReviewsByHallIdAsync(int hallId)
    {
        return await _context.Reviews
            .Where(r => r.HallId == hallId)
            .Include(r => r.Customer)
                .ThenInclude(c => c.AppUser)
            .OrderByDescending(r => r.Created)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetReviewsByCustomerIdAsync(int customerId)
    {
        return await _context.Reviews
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.Created)
            .ToListAsync();
    }

    public async Task<Review> GetReviewWithDetailsAsync(int reviewId)
    {
        return await _context.Reviews
            .Include(r => r.Customer)
                .ThenInclude(c => c.AppUser)
            .FirstOrDefaultAsync(r => r.Id == reviewId);
    }

    public async Task<double> GetAverageRatingByHallIdAsync(int hallId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.HallId == hallId)
            .ToListAsync();

        if (!reviews.Any())
            return 0;

        return reviews.Average(r => r.Rating);
    }

    public async Task<IEnumerable<Review>> GetRecentReviewsAsync(int count = 10)
    {
        return await _context.Reviews
            .Include(r => r.Customer)
                .ThenInclude(c => c.AppUser)
            .OrderByDescending(r => r.Created)
            .Take(count)
            .ToListAsync();
    }

    public async Task<bool> CustomerHasReviewedHallAsync(int customerId, int hallId)
    {
        return await _context.Reviews
            .AnyAsync(r => r.CustomerId == customerId && r.HallId == hallId);
    }
}
