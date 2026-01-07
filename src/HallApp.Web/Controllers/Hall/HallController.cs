using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using HallApp.Core.Interfaces.IServices;
using HallApp.Application.DTOs.Champer.Hall;
using HallApp.Web.Controllers.Common;
using HallApp.Application.Common.Models;
using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Web.Controllers.Hall
{
    /// <summary>
    /// Public Hall management controller for customer browsing
    /// Handles hall retrieval for customers and guests
    /// </summary>
    [AllowAnonymous]
    [Route("api/halls")]
    public class HallController : BaseApiController
    {
        private readonly IHallService _hallService;
        private readonly IMapper _mapper;

        public HallController(IHallService hallService, IMapper mapper)
        {
            _hallService = hallService;
            _mapper = mapper;
        }

        /// <summary>
        /// Get all halls for public browsing with pagination
        /// </summary>
        /// <param name="hallParams">Pagination and filter parameters</param>
        /// <returns>Paginated list of halls</returns>
        [HttpGet]
        public async Task<ActionResult<PaginatedApiResponse<HallDto>>> GetHalls([FromQuery] HallParams hallParams)
        {
            try
            {
                var hallEntities = await _hallService.GetAllHallsAsync();
                if (hallEntities == null || !hallEntities.Any())
                {
                    return new PaginatedApiResponse<HallDto>
                    {
                        StatusCode = 200,
                        Message = "No halls found",
                        IsSuccess = true,
                        Data = new List<HallDto>(),
                        CurrentPage = hallParams.PageNumber,
                        PageSize = hallParams.PageSize,
                        TotalCount = 0,
                        TotalPages = 0
                    };
                }

                // Apply filtering
                var filteredHalls = hallEntities.AsQueryable();
                
                if (!string.IsNullOrEmpty(hallParams.SearchTerm))
                {
                    filteredHalls = filteredHalls.Where(h => 
                        h.Name.ToLower().Contains(hallParams.SearchTerm) ||
                        h.Description.ToLower().Contains(hallParams.SearchTerm));
                }

                if (hallParams.MinCapacity.HasValue)
                {
                    filteredHalls = filteredHalls.Where(h => 
                        Math.Max(h.MaleMax, h.FemaleMax) >= hallParams.MinCapacity);
                }

                if (hallParams.MaxCapacity.HasValue)
                {
                    filteredHalls = filteredHalls.Where(h => 
                        Math.Min(h.MaleMin, h.FemaleMin) <= hallParams.MaxCapacity);
                }

                // Apply sorting
                filteredHalls = hallParams.OrderBy?.ToLower() switch
                {
                    "capacity" => filteredHalls.OrderBy(h => Math.Max(h.MaleMax, h.FemaleMax)),
                    "name" => filteredHalls.OrderBy(h => h.Name),
                    _ => filteredHalls.OrderBy(h => h.Name)
                };

                var totalCount = filteredHalls.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / hallParams.PageSize);

                // Apply pagination
                var pagedHalls = filteredHalls
                    .Skip((hallParams.PageNumber - 1) * hallParams.PageSize)
                    .Take(hallParams.PageSize)
                    .ToList();

                var hallDtos = _mapper.Map<List<HallDto>>(pagedHalls);

                return new PaginatedApiResponse<HallDto>
                {
                    StatusCode = 200,
                    Message = "Halls retrieved successfully",
                    IsSuccess = true,
                    Data = hallDtos,
                    CurrentPage = hallParams.PageNumber,
                    PageSize = hallParams.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PaginatedApiResponse<HallDto>
                {
                    StatusCode = 500,
                    Message = $"Failed to retrieve halls: {ex.Message}",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Get hall by ID for public viewing
        /// </summary>
        /// <param name="id">Hall ID</param>
        /// <returns>Hall details</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<HallDto>>> GetHallById(int id)
        {
            try
            {
                var hall = await _hallService.GetHallByIdAsync(id);
                if (hall == null)
                {
                    return Error<HallDto>("Hall not found", 404);
                }

                var hallDto = _mapper.Map<HallDto>(hall);
                return Success(hallDto, "Hall retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<HallDto>($"Failed to retrieve hall: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Search halls by name or description
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <returns>List of matching halls</returns>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> SearchHalls([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return Error<IEnumerable<HallDto>>("Search term is required", 400);
                }

                var hallEntities = await _hallService.SearchHallsAsync(searchTerm);
                var halls = _mapper.Map<List<HallDto>>(hallEntities);
                
                return Success<IEnumerable<HallDto>>(halls, $"Found {halls.Count} halls matching '{searchTerm}'");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<HallDto>>($"Failed to search halls: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get alternative halls available for the same date when one is rejected
        /// </summary>
        /// <param name="eventDate">Event date</param>
        /// <param name="startTime">Start time</param>
        /// <param name="endTime">End time</param>
        /// <param name="genderPreference">Gender preference (0=Male, 1=Female, 2=Both)</param>
        /// <returns>List of available alternative halls</returns>
        [AllowAnonymous]
        [HttpGet("alternatives")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetAlternativeHalls(
            [FromQuery] DateTime eventDate, 
            [FromQuery] TimeSpan startTime, 
            [FromQuery] TimeSpan endTime,
            [FromQuery] int genderPreference = 2)
        {
            try
            {
                var startDateTime = eventDate.Add(startTime);
                var endDateTime = eventDate.Add(endTime);
                
                var allHalls = await _hallService.GetAllHallsAsync();
                var availableHalls = new List<HallApp.Core.Entities.ChamperEntities.Hall>();
                
                foreach (var hall in allHalls)
                {
                    // Check gender compatibility
                    if (genderPreference != 2 && hall.Gender != genderPreference && hall.Gender != 2)
                        continue;
                        
                    // Check availability
                    var isAvailable = await _hallService.IsHallAvailableAsync(hall.ID, startDateTime, endDateTime);
                    if (isAvailable)
                    {
                        availableHalls.Add(hall);
                    }
                }
                
                var hallDtos = _mapper.Map<List<HallDto>>(availableHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} alternative halls for {eventDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<HallDto>>($"Failed to get alternative halls: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get special offer halls (halls with active discounts/offers)
        /// </summary>
        /// <param name="limit">Maximum number of halls to return</param>
        /// <returns>List of special offer halls</returns>
        [HttpGet("special-offers")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetSpecialOfferHalls([FromQuery] int limit = 6)
        {
            try
            {
                var allHalls = await _hallService.GetAllHallsAsync();
                // Filter by HasSpecialOffer flag (set when hall has active discounts/offers)
                var specialOfferHalls = allHalls
                    .Where(h => h.HasSpecialOffer && h.Active)
                    .OrderByDescending(h => h.AverageRating)
                    .Take(limit)
                    .ToList();

                var hallDtos = _mapper.Map<List<HallDto>>(specialOfferHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} special offer halls");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<HallDto>>($"Failed to get special offer halls: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get featured halls (halls that paid for featured placement)
        /// </summary>
        /// <param name="limit">Maximum number of halls to return</param>
        /// <returns>List of featured halls</returns>
        [HttpGet("featured")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetFeaturedHalls([FromQuery] int limit = 6)
        {
            try
            {
                var allHalls = await _hallService.GetAllHallsAsync();
                // Filter by IsFeatured flag (set when hall owner pays for featured placement)
                var featuredHalls = allHalls
                    .Where(h => h.IsFeatured && h.Active)
                    .OrderByDescending(h => h.AverageRating)
                    .ThenByDescending(h => Math.Max(h.MaleMax, h.FemaleMax))
                    .Take(limit)
                    .ToList();

                var hallDtos = _mapper.Map<List<HallDto>>(featuredHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} featured halls");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<HallDto>>($"Failed to get featured halls: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get popular halls (high rating and multiple reviews)
        /// </summary>
        /// <param name="limit">Maximum number of halls to return</param>
        /// <returns>List of popular halls</returns>
        [HttpGet("popular")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetPopularHalls([FromQuery] int limit = 6)
        {
            try
            {
                var allHalls = await _hallService.GetAllHallsAsync();
                var popularHalls = allHalls
                    .Where(h => h.Reviews != null && 
                               h.Reviews.Count >= 5 && 
                               h.AverageRating >= 3.8 && 
                               h.Active)
                    .OrderByDescending(h => h.Reviews.Count)
                    .ThenByDescending(h => h.AverageRating)
                    .Take(limit)
                    .ToList();

                var hallDtos = _mapper.Map<List<HallDto>>(popularHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} popular halls");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<HallDto>>($"Failed to get popular halls: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get premium halls (halls with premium subscription)
        /// </summary>
        /// <param name="limit">Maximum number of halls to return</param>
        /// <returns>List of premium halls</returns>
        [HttpGet("premium")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetPremiumHalls([FromQuery] int limit = 6)
        {
            try
            {
                var allHalls = await _hallService.GetAllHallsAsync();
                // Filter by IsPremium flag (set when hall has premium subscription)
                var premiumHalls = allHalls
                    .Where(h => h.IsPremium && h.Active)
                    .OrderByDescending(h => h.AverageRating)
                    .ThenByDescending(h => h.MediaFiles?.Count ?? 0)
                    .Take(limit)
                    .ToList();

                var hallDtos = _mapper.Map<List<HallDto>>(premiumHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} premium halls");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<HallDto>>($"Failed to get premium halls: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get newly added halls (recently created halls)
        /// </summary>
        /// <param name="limit">Maximum number of halls to return</param>
        /// <returns>List of newly added halls</returns>
        [HttpGet("newly-added")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetNewlyAddedHalls([FromQuery] int limit = 6)
        {
            try
            {
                var allHalls = await _hallService.GetAllHallsAsync();
                var newlyAddedHalls = allHalls
                    .Where(h => h.Active)
                    .OrderByDescending(h => h.Created)
                    .Take(limit)
                    .ToList();

                var hallDtos = _mapper.Map<List<HallDto>>(newlyAddedHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} newly added halls");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<HallDto>>($"Failed to get newly added halls: {ex.Message}", 500);
            }
        }
    }
}
