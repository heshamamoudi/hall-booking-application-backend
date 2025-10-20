using AutoMapper;
using HallApp.Web.Controllers.Common;
using HallApp.Application.DTOs.Review;
using HallApp.Application.Common.Models;
using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Review
{
    /// <summary>
    /// Review management controller
    /// Handles hall reviews and ratings from customers
    /// </summary>
    [Route("api/reviews")]
    public class ReviewController : BaseApiController
    {
        private readonly IReviewService _reviewService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public ReviewController(
            IReviewService reviewService,
            INotificationService notificationService,
            IMapper mapper)
        {
            _reviewService = reviewService;
            _notificationService = notificationService;
            _mapper = mapper;
        }

        /// <summary>
        /// Get review by ID
        /// </summary>
        /// <param name="id">Review ID</param>
        /// <returns>Review details</returns>
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> GetReviewById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Error<ReviewDto>("Invalid review ID", 400);
                }

                var review = await _reviewService.GetReviewByIdAsync(id);
                
                if (review == null)
                {
                    return Error<ReviewDto>($"Review with ID {id} not found", 404);
                }

                var reviewDto = _mapper.Map<ReviewDto>(review);
                return Success(reviewDto, "Review retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<ReviewDto>($"Failed to retrieve review: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get reviews for a specific hall
        /// </summary>
        /// <param name="hallId">Hall ID</param>
        /// <returns>List of reviews for the hall</returns>
        [AllowAnonymous]
        [HttpGet("hall/{hallId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ReviewDto>>>> GetReviewsByHallId(int hallId)
        {
            try
            {
                if (hallId <= 0)
                {
                    return Error<IEnumerable<ReviewDto>>("Invalid hall ID", 400);
                }

                var reviews = await _reviewService.GetReviewsByHallIdAsync(hallId);
                var reviewDtos = _mapper.Map<IEnumerable<ReviewDto>>(reviews);
                return Success(reviewDtos, "Reviews retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ReviewDto>>($"Failed to retrieve reviews: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get reviews by current customer
        /// </summary>
        /// <returns>List of customer's reviews</returns>
        [Authorize(Roles = "Customer")]
        [HttpGet("my-reviews")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ReviewDto>>>> GetMyReviews()
        {
            try
            {
                var reviews = await _reviewService.GetReviewsByCustomerIdAsync(UserId.ToString());
                
                if (reviews == null || !reviews.Any())
                {
                    return Success<IEnumerable<ReviewDto>>(new List<ReviewDto>(), "You haven't written any reviews yet");
                }

                var reviewDtos = _mapper.Map<IEnumerable<ReviewDto>>(reviews);
                return Success<IEnumerable<ReviewDto>>(reviewDtos, "Your reviews retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ReviewDto>>($"Failed to retrieve customer reviews: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Create new review (Customer only)
        /// </summary>
        /// <param name="reviewDto">Review data</param>
        /// <returns>Created review</returns>
        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> CreateReview([FromBody] RegisterReviewDto reviewDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<ReviewDto>($"Invalid review data: {errors}", 400);
                }

                // Set the customer ID from authenticated user
                reviewDto.CustomerId = UserId;

                // Validate the customer has a confirmed booking for this hall
                var isValid = await _reviewService.HasConfirmedBooking(reviewDto.HallId, UserId.ToString());
                if (!isValid)
                {
                    return Error<ReviewDto>("You cannot leave a review without a confirmed booking for this hall", 400);
                }

                // Check if customer already reviewed this hall
                var existingReview = await _reviewService.GetReviewByCustomerAndHall(UserId.ToString(), reviewDto.HallId);
                if (existingReview != null)
                {
                    return Error<ReviewDto>("You have already reviewed this hall", 400);
                }

                var reviewEntity = _mapper.Map<HallApp.Core.Entities.ReviewEntities.Review>(reviewDto);
                var review = await _reviewService.CreateReviewAsync(reviewEntity);

                if (review == null)
                {
                    return Error<ReviewDto>("Failed to create review", 500);
                }

                // Send notification for review creation (async)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            UserId,
                            "Review Created",
                            $"Your review for Hall ID {reviewDto.HallId} has been submitted successfully. Thank you for your feedback!",
                            "Review"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                });

                var createdReviewDto = _mapper.Map<ReviewDto>(review);
                return Success(createdReviewDto, "Review created successfully");
            }
            catch (Exception ex)
            {
                return Error<ReviewDto>($"Failed to create review: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update existing review
        /// </summary>
        /// <param name="id">Review ID</param>
        /// <param name="reviewDto">Updated review data</param>
        /// <returns>Updated review</returns>
        [Authorize(Roles = "Customer")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> UpdateReview(int id, [FromBody] ReviewDto reviewDto)
        {
            try
            {
                if (id <= 0)
                {
                    return Error<ReviewDto>("Invalid review ID", 400);
                }

                if (id != reviewDto.Id)
                {
                    return Error<ReviewDto>("Review ID mismatch", 400);
                }

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<ReviewDto>($"Invalid review data: {errors}", 400);
                }

                var existingReview = await _reviewService.GetReviewByIdAsync(id);
                if (existingReview == null)
                {
                    return Error<ReviewDto>($"Review with ID {id} not found", 404);
                }

                // Check if the review belongs to the current customer (unless admin)
                if (!IsAdmin && existingReview.CustomerId != UserId)
                {
                    return Error<ReviewDto>("You can only update your own reviews", 403);
                }

                ReviewDto updatedReview;
                if (IsAdmin)
                {
                    // Admin can update all fields
                    var reviewEntity = _mapper.Map<HallApp.Core.Entities.ReviewEntities.Review>(reviewDto);
                    var updatedEntity = await _reviewService.UpdateReviewAsync(reviewEntity);
                    updatedReview = _mapper.Map<ReviewDto>(updatedEntity);
                }
                else if (existingReview.CustomerId == UserId)
                {
                    // User can only update their own review
                    var reviewEntity = _mapper.Map<HallApp.Core.Entities.ReviewEntities.Review>(reviewDto);
                    var updatedEntity = await _reviewService.UpdateUserReviewAsync(UserId.ToString(), reviewEntity);
                    updatedReview = _mapper.Map<ReviewDto>(updatedEntity);
                }
                else
                {
                    return Error<ReviewDto>("You can only update your own reviews", 403);
                }

                if (updatedReview == null)
                {
                    return Error<ReviewDto>("Failed to update review", 500);
                }

                return Success(updatedReview, "Review updated successfully");
            }
            catch (Exception ex)
            {
                return Error<ReviewDto>($"Failed to update review: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Delete review
        /// </summary>
        /// <param name="id">Review ID</param>
        /// <returns>Success response</returns>
        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteReview(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Error<string>("Invalid review ID", 400);
                }

                // Get the review entity to check permissions
                var review = await _reviewService.GetReviewEntityById(id);
                if (review == null)
                {
                    return Error<string>($"Review with ID {id} not found", 404);
                }

                // Check permissions - customer can delete own reviews, admin can delete any
                if (!IsAdmin && review.CustomerId.ToString() != UserId.ToString())
                {
                    return Error<string>("You can only delete your own reviews", 403);
                }

                await _reviewService.DeleteReview(review);
                
                // Send notification for review deletion (async)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            (int)review.CustomerId,
                            "Review Deleted",
                            $"Your review for Hall ID {review.HallId} has been deleted.",
                            "Review"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                });

                return Success<string>("Review deleted successfully", "Review deleted successfully");
            }
            catch (Exception ex)
            {
                return Error<string>($"Failed to delete review: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get hall rating statistics
        /// </summary>
        /// <param name="hallId">Hall ID</param>
        /// <returns>Rating statistics for the hall</returns>
        [AllowAnonymous]
        [HttpGet("hall/{hallId:int}/statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetHallReviewStatistics(int hallId)
        {
            try
            {
                if (hallId <= 0)
                {
                    return Error<object>("Invalid hall ID", 400);
                }

                var stats = await _reviewService.GetReviewStatisticsAsync();
                return Success<object>(stats, "Hall review statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to retrieve hall review statistics: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get all reviews (Admin only)
        /// </summary>
        /// <returns>List of all reviews</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/all")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ReviewDto>>>> GetAllReviews()
        {
            try
            {
                var reviews = await _reviewService.GetAllReviewsAsync();
                var reviewDtos = _mapper.Map<IEnumerable<ReviewDto>>(reviews);
                return Success(reviewDtos, "All reviews retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ReviewDto>>($"Failed to retrieve all reviews: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get reviews requiring moderation (Admin only)
        /// </summary>
        /// <returns>List of flagged reviews</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/flagged")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ReviewDto>>>> GetFlaggedReviews()
        {
            try
            {
                var flaggedReviews = await _reviewService.GetFlaggedReviewsAsync();
                var reviewDtos = _mapper.Map<IEnumerable<ReviewDto>>(flaggedReviews);
                return Success<IEnumerable<ReviewDto>>(reviewDtos, "Customer reviews retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ReviewDto>>($"Failed to retrieve flagged reviews: {ex.Message}", 500);
            }
        }
    }
}
