using HallApp.Application.DTOs.Review;
using HallApp.Core.Entities.ReviewEntities;
using HallApp.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// Service interface for hall manager review management operations.
/// Separated from IReviewService to follow ISP - this handles the manager perspective,
/// while IReviewService handles customer-facing review CRUD.
/// Defined in Application layer (same pattern as IHallBlockedDateService) because it
/// returns Application DTOs.
/// </summary>
public interface IHallReviewManagementService
{
    /// <summary>
    /// Gets reviews for a specific hall with optional filtering and pagination.
    /// Includes customer details and manager response data.
    /// </summary>
    Task<(IEnumerable<ManagerReviewDto> Reviews, int TotalCount)> GetHallReviewsAsync(
        int hallId,
        int? rating = null,
        bool? hasResponse = null,
        bool? isFlagged = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 10,
        string sortBy = "created",
        bool sortDescending = true);

    /// <summary>
    /// Gets review statistics/metrics for a specific hall.
    /// </summary>
    Task<ManagerReviewStatsDto> GetHallReviewStatsAsync(int hallId);

    /// <summary>
    /// Adds a manager response to a review.
    /// Business rule: only one response per review.
    /// </summary>
    Task<ManagerReviewDto> RespondToReviewAsync(int reviewId, string responseText, int responderId);

    /// <summary>
    /// Flags a review as inappropriate for admin review.
    /// </summary>
    Task<bool> FlagReviewAsync(int reviewId, string reason, int flaggedByUserId);

    /// <summary>
    /// Gets a single review by ID with full details for the manager view.
    /// </summary>
    Task<ManagerReviewDto?> GetReviewByIdAsync(int reviewId);
}

/// <summary>
/// Service for hall manager review management operations.
/// Uses IUnitOfWork for database access following existing codebase patterns.
/// All queries use sequential execution to avoid DbContext concurrency issues.
/// </summary>
public class HallReviewManagementService : IHallReviewManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HallReviewManagementService> _logger;
    private readonly HtmlSanitizerService _htmlSanitizerService;

    public HallReviewManagementService(
        IUnitOfWork unitOfWork,
        ILogger<HallReviewManagementService> logger,
        HtmlSanitizerService htmlSanitizerService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _htmlSanitizerService = htmlSanitizerService;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<ManagerReviewDto> Reviews, int TotalCount)> GetHallReviewsAsync(
        int hallId,
        int? rating = null,
        bool? hasResponse = null,
        bool? isFlagged = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 10,
        string sortBy = "created",
        bool sortDescending = true)
    {
        _logger.LogInformation(
            "Fetching reviews for Hall {HallId} with filters - Rating: {Rating}, HasResponse: {HasResponse}, Page: {Page}",
            hallId, rating, hasResponse, page);

        // Get all reviews for this hall with customer details loaded
        var allReviews = await _unitOfWork.ReviewRepository.GetReviewsByHallIdAsync(hallId);
        var reviewsList = allReviews.ToList();

        // Get the hall name
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        var hallName = hall?.Name ?? string.Empty;

        // Apply filters in-memory (reviews per hall are typically not in the millions)
        IEnumerable<Review> filtered = reviewsList;

        if (rating.HasValue)
        {
            filtered = filtered.Where(r => r.Rating == rating.Value);
        }

        if (hasResponse.HasValue)
        {
            filtered = hasResponse.Value
                ? filtered.Where(r => !string.IsNullOrEmpty(r.ManagerResponse))
                : filtered.Where(r => string.IsNullOrEmpty(r.ManagerResponse));
        }

        if (isFlagged.HasValue)
        {
            filtered = filtered.Where(r => r.IsFlagged == isFlagged.Value);
        }

        if (fromDate.HasValue)
        {
            filtered = filtered.Where(r => r.Created >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            filtered = filtered.Where(r => r.Created <= toDate.Value);
        }

        // Get total count before pagination
        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        // Apply sorting
        filteredList = sortBy.ToLowerInvariant() switch
        {
            "rating" => sortDescending
                ? filteredList.OrderByDescending(r => r.Rating).ToList()
                : filteredList.OrderBy(r => r.Rating).ToList(),
            "responded" => sortDescending
                ? filteredList.OrderByDescending(r => r.ManagerResponseDate.HasValue).ThenByDescending(r => r.ManagerResponseDate).ToList()
                : filteredList.OrderBy(r => r.ManagerResponseDate.HasValue).ThenBy(r => r.ManagerResponseDate).ToList(),
            _ => sortDescending
                ? filteredList.OrderByDescending(r => r.Created).ToList()
                : filteredList.OrderBy(r => r.Created).ToList()
        };

        // Apply pagination
        var paged = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Map to DTOs
        var dtos = paged.Select(r => MapToManagerReviewDto(r, hallName)).ToList();

        return (dtos, totalCount);
    }

    /// <inheritdoc />
    public async Task<ManagerReviewStatsDto> GetHallReviewStatsAsync(int hallId)
    {
        _logger.LogInformation("Fetching review statistics for Hall {HallId}", hallId);

        // Get hall details
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        var hallName = hall?.Name ?? string.Empty;

        // Get all reviews for this hall (sequential query - no concurrency issues)
        var reviews = await _unitOfWork.ReviewRepository.GetReviewsByHallIdAsync(hallId);
        var reviewsList = reviews.ToList();

        var stats = new ManagerReviewStatsDto
        {
            HallId = hallId,
            HallName = hallName,
            TotalReviews = reviewsList.Count,
            AverageRating = reviewsList.Count > 0 ? Math.Round(reviewsList.Average(r => r.Rating), 1) : 0,
            RespondedCount = reviewsList.Count(r => !string.IsNullOrEmpty(r.ManagerResponse)),
            NotRespondedCount = reviewsList.Count(r => string.IsNullOrEmpty(r.ManagerResponse)),
            FlaggedCount = reviewsList.Count(r => r.IsFlagged),
            RatingDistribution = new Dictionary<int, int>
            {
                { 1, reviewsList.Count(r => r.Rating == 1) },
                { 2, reviewsList.Count(r => r.Rating == 2) },
                { 3, reviewsList.Count(r => r.Rating == 3) },
                { 4, reviewsList.Count(r => r.Rating == 4) },
                { 5, reviewsList.Count(r => r.Rating == 5) }
            }
        };

        return stats;
    }

    /// <inheritdoc />
    public async Task<ManagerReviewDto> RespondToReviewAsync(
        int reviewId, string responseText, int responderId)
    {
        _logger.LogInformation(
            "Manager {ResponderId} responding to Review {ReviewId}", responderId, reviewId);

        // Get the review
        var review = await _unitOfWork.ReviewRepository.GetReviewWithDetailsAsync(reviewId);
        if (review == null)
        {
            throw new KeyNotFoundException($"Review with ID {reviewId} not found");
        }

        // Check if already responded
        if (!string.IsNullOrEmpty(review.ManagerResponse))
        {
            throw new InvalidOperationException("This review already has a manager response. Only one response per review is allowed.");
        }

        // Sanitize the response text to prevent stored XSS
        responseText = _htmlSanitizerService.Sanitize(responseText);

        // Set the response fields
        review.ManagerResponse = responseText;
        review.ManagerResponseDate = DateTime.UtcNow;
        review.ManagerResponderId = responderId;
        review.Updated = DateTime.UtcNow;

        _unitOfWork.ReviewRepository.Update(review);
        await _unitOfWork.Complete();

        _logger.LogInformation(
            "Manager {ResponderId} successfully responded to Review {ReviewId}", responderId, reviewId);

        // Get the hall name for the DTO
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(review.HallId);
        var hallName = hall?.Name ?? string.Empty;

        // Get responder name
        var responder = await _unitOfWork.UserRepository.GetUserById(responderId);
        var responderName = responder != null
            ? $"{responder.FirstName} {responder.LastName}".Trim()
            : string.Empty;

        var dto = MapToManagerReviewDto(review, hallName);
        dto.ManagerResponderName = responderName;

        return dto;
    }

    /// <inheritdoc />
    public async Task<bool> FlagReviewAsync(int reviewId, string reason, int flaggedByUserId)
    {
        _logger.LogInformation(
            "User {UserId} flagging Review {ReviewId} with reason: {Reason}",
            flaggedByUserId, reviewId, reason);

        var review = await _unitOfWork.ReviewRepository.GetByIdAsync(reviewId);
        if (review == null)
        {
            return false;
        }

        // Sanitize the flag reason to prevent stored XSS
        reason = _htmlSanitizerService.Sanitize(reason);

        review.IsFlagged = true;
        review.FlagReason = reason;
        review.FlaggedDate = DateTime.UtcNow;
        review.FlaggedByUserId = flaggedByUserId;
        review.Updated = DateTime.UtcNow;

        _unitOfWork.ReviewRepository.Update(review);
        await _unitOfWork.Complete();

        _logger.LogInformation(
            "Review {ReviewId} flagged successfully by User {UserId}", reviewId, flaggedByUserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<ManagerReviewDto?> GetReviewByIdAsync(int reviewId)
    {
        var review = await _unitOfWork.ReviewRepository.GetReviewWithDetailsAsync(reviewId);
        if (review == null)
        {
            return null;
        }

        var hall = await _unitOfWork.HallRepository.GetByIdAsync(review.HallId);
        var hallName = hall?.Name ?? string.Empty;

        return MapToManagerReviewDto(review, hallName);
    }

    /// <summary>
    /// Maps a Review entity to a ManagerReviewDto.
    /// </summary>
    private static ManagerReviewDto MapToManagerReviewDto(Review review, string hallName)
    {
        var customerName = string.Empty;
        if (review.Customer?.AppUser != null)
        {
            customerName = $"{review.Customer.AppUser.FirstName} {review.Customer.AppUser.LastName}".Trim();
            if (string.IsNullOrEmpty(customerName))
            {
                customerName = review.Customer.AppUser.UserName ?? review.Customer.AppUser.Email ?? "Unknown";
            }
        }

        // Resolve manager responder name from the navigation property if loaded
        string? managerResponderName = null;
        if (review.ManagerResponder != null)
        {
            managerResponderName = $"{review.ManagerResponder.FirstName} {review.ManagerResponder.LastName}".Trim();
            if (string.IsNullOrEmpty(managerResponderName))
            {
                managerResponderName = review.ManagerResponder.UserName ?? review.ManagerResponder.Email;
            }
        }

        return new ManagerReviewDto
        {
            Id = review.Id,
            HallId = review.HallId,
            HallName = hallName,
            Rating = review.Rating,
            Content = review.Content,
            Created = review.Created,
            Updated = review.Updated,
            IsApproved = review.IsApproved,
            IsFlagged = review.IsFlagged,
            CustomerId = review.CustomerId,
            CustomerName = customerName,
            ManagerResponse = review.ManagerResponse,
            ManagerResponseDate = review.ManagerResponseDate,
            ManagerResponderName = managerResponderName,
            FlagReason = review.FlagReason,
            FlaggedDate = review.FlaggedDate
        };
    }
}
