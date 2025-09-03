using AutoMapper;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities.ReviewEntities;

namespace HallApp.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReviewService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Review> GetReviewByIdAsync(int reviewId)
    {
        return await _unitOfWork.ReviewRepository.GetByIdAsync(reviewId);
    }

    public async Task<IEnumerable<Review>> GetReviewsByUserIdAsync(string userId)
    {
        if (int.TryParse(userId, out int customerId))
        {
            return await _unitOfWork.ReviewRepository.GetReviewsByCustomerIdAsync(customerId);
        }
        return new List<Review>();
    }

    public async Task<IEnumerable<Review>> GetReviewsByHallIdAsync(int hallId)
    {
        return await _unitOfWork.ReviewRepository.GetReviewsByHallIdAsync(hallId);
    }

    public async Task<IEnumerable<Review>> GetAllReviewsAsync()
    {
        return await _unitOfWork.ReviewRepository.GetAllAsync();
    }

    public async Task<Review> CreateReviewAsync(Review review)
    {
        review.Created = DateTime.UtcNow;
        review.Updated = DateTime.UtcNow;
        await _unitOfWork.ReviewRepository.AddAsync(review);
        await _unitOfWork.Complete();
        return review;
    }

    public async Task<Review> UpdateReviewAsync(Review review)
    {
        var existingReview = await _unitOfWork.ReviewRepository.GetByIdAsync(review.Id);
        if (existingReview == null) return new Review();
        
        existingReview.Rating = review.Rating;
        existingReview.Content = review.Content;
        existingReview.Updated = DateTime.UtcNow;
        
        _unitOfWork.ReviewRepository.Update(existingReview);
        await _unitOfWork.Complete();
        return existingReview;
    }

    public async Task<Review> GetReviewEntityById(int reviewId)
    {
        return await _unitOfWork.ReviewRepository.GetReviewWithDetailsAsync(reviewId);
    }

    public async Task DeleteReview(Review review)
    {
        _unitOfWork.ReviewRepository.Delete(review);
        await _unitOfWork.Complete();
    }

    public async Task<Review> UpdateUserReviewAsync(string userId, Review review)
    {
        var existingReview = await _unitOfWork.ReviewRepository.GetByIdAsync(review.Id);
        if (existingReview == null || existingReview.CustomerId.ToString() != userId) 
            return new Review();
        
        existingReview.Rating = review.Rating;
        existingReview.Content = review.Content;
        existingReview.Updated = DateTime.UtcNow;
        
        _unitOfWork.ReviewRepository.Update(existingReview);
        await _unitOfWork.Complete();
        return existingReview;
    }

    public async Task<bool> DeleteReviewAsync(int reviewId)
    {
        var review = await _unitOfWork.ReviewRepository.GetByIdAsync(reviewId);
        if (review == null) return false;
        
        _unitOfWork.ReviewRepository.Delete(review);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> ToggleReviewApprovalAsync(int reviewId)
    {
        var review = await _unitOfWork.ReviewRepository.GetByIdAsync(reviewId);
        if (review == null) return false;
        
        review.IsApproved = !review.IsApproved;
        review.Updated = DateTime.UtcNow;
        
        _unitOfWork.ReviewRepository.Update(review);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> FlagReviewAsync(int reviewId)
    {
        var review = await _unitOfWork.ReviewRepository.GetByIdAsync(reviewId);
        if (review == null) return false;
        
        review.IsFlagged = true;
        review.Updated = DateTime.UtcNow;
        
        _unitOfWork.ReviewRepository.Update(review);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<IEnumerable<Review>> GetPendingReviewsAsync()
    {
        var allReviews = await _unitOfWork.ReviewRepository.GetAllAsync();
        return allReviews.Where(r => !r.IsApproved).ToList();
    }

    public async Task<IEnumerable<Review>> GetFlaggedReviewsAsync()
    {
        var allReviews = await _unitOfWork.ReviewRepository.GetAllAsync();
        return allReviews.Where(r => r.IsFlagged).ToList();
    }

    public async Task<bool> HasConfirmedBooking(int hallId, string customerId)
    {
        if (int.TryParse(customerId, out int customerIdInt))
        {
            // Check if customer has any confirmed bookings for this hall
            var bookings = await _unitOfWork.BookingRepository.GetAllAsync();
            return bookings.Any(b => b.CustomerId == customerIdInt && b.HallId == hallId && 
                                   (b.Status == "Confirmed" || b.Status == "Completed"));
        }
        return false;
    }

    public async Task<Review> GetReviewByCustomerAndHall(string customerId, int hallId)
    {
        if (int.TryParse(customerId, out int customerIdInt))
        {
            var reviews = await _unitOfWork.ReviewRepository.GetReviewsByCustomerIdAsync(customerIdInt);
            return reviews.FirstOrDefault(r => r.HallId == hallId) ?? new Review();
        }
        return new Review();
    }

    public async Task<IEnumerable<Review>> GetReviewsByRatingAsync(int rating)
    {
        var allReviews = await _unitOfWork.ReviewRepository.GetAllAsync();
        return allReviews.Where(r => r.Rating == rating).ToList();
    }

    public async Task<double> GetAverageRatingForHallAsync(int hallId)
    {
        return await _unitOfWork.ReviewRepository.GetAverageRatingByHallIdAsync(hallId);
    }

    public async Task<IEnumerable<Review>> GetRecentReviewsAsync(int count)
    {
        return await _unitOfWork.ReviewRepository.GetRecentReviewsAsync(count);
    }

    public async Task<bool> ApproveReviewAsync(int reviewId)
    {
        var review = await _unitOfWork.ReviewRepository.GetByIdAsync(reviewId);
        if (review == null) return false;
        
        review.IsApproved = true;
        review.IsFlagged = false; // Clear flag when approving
        review.RejectionReason = null;
        review.Updated = DateTime.UtcNow;
        
        _unitOfWork.ReviewRepository.Update(review);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> RejectReviewAsync(int reviewId, string reason)
    {
        var review = await _unitOfWork.ReviewRepository.GetByIdAsync(reviewId);
        if (review == null) return false;
        
        review.IsApproved = false;
        review.RejectionReason = reason;
        review.Updated = DateTime.UtcNow;
        
        _unitOfWork.ReviewRepository.Update(review);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<IEnumerable<Review>> SearchReviewsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<Review>();
            
        var allReviews = await _unitOfWork.ReviewRepository.GetAllAsync();
        return allReviews.Where(r => 
            r.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    public async Task<bool> ValidateReviewPermissionsAsync(int reviewId, string userId)
    {
        var review = await _unitOfWork.ReviewRepository.GetByIdAsync(reviewId);
        if (review == null) return false;
        
        // Check if user owns the review
        return review.CustomerId.ToString() == userId;
    }

    public async Task<int> GetReviewCountForHallAsync(int hallId)
    {
        var reviews = await _unitOfWork.ReviewRepository.GetReviewsByHallIdAsync(hallId);
        return reviews.Count();
    }

    public async Task<IEnumerable<Review>> GetReviewsByCustomerIdAsync(string customerId)
    {
        if (int.TryParse(customerId, out int customerIdInt))
        {
            return await _unitOfWork.ReviewRepository.GetReviewsByCustomerIdAsync(customerIdInt);
        }
        return new List<Review>();
    }

    public async Task<object> GetReviewStatisticsAsync()
    {
        var allReviews = await _unitOfWork.ReviewRepository.GetAllAsync();
        var reviewsList = allReviews.ToList();
        
        return new
        {
            TotalReviews = reviewsList.Count,
            AverageRating = reviewsList.Any() ? reviewsList.Average(r => r.Rating) : 0.0,
            PendingReviews = reviewsList.Count(r => !r.IsApproved),
            ApprovedReviews = reviewsList.Count(r => r.IsApproved),
            RejectedReviews = 0 // Assuming no rejection status field exists
        };
    }
}
