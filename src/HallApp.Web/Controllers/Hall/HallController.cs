using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using HallApp.Core.Interfaces.IServices;
using HallApp.Application.DTOs.Halls.Hall;
using HallApp.Web.Controllers.Common;
using HallApp.Core.Exceptions;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Web.Services;
using HallApp.Web.DTOs;

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
        private readonly IFileUploadService _fileUploadService;

        public HallController(IHallService hallService, IMapper mapper, IFileUploadService fileUploadService)
        {
            _hallService = hallService;
            _mapper = mapper;
            _fileUploadService = fileUploadService;
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
                    .Where(h => h.HasSpecialOffer && h.IsActive)
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
                    .Where(h => h.IsFeatured && h.IsActive)
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
                               h.IsActive)
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
                    .Where(h => h.IsPremium && h.IsActive)
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
                    .Where(h => h.IsActive)
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

        #region Hall Manager CRUD Operations

        /// <summary>
        /// Create a new hall (HallManager only)
        /// </summary>
        /// <param name="createHallDto">Hall creation data</param>
        /// <returns>Created hall</returns>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<HallDto>>> CreateHall([FromBody] HallCreateDto createHallDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<HallDto>($"Invalid data: {errors}", 400);
                }

                var hallEntity = _mapper.Map<HallApp.Core.Entities.ChamperEntities.Hall>(createHallDto);
                var createdHall = await _hallService.CreateHallAsync(hallEntity);
                var hallDto = _mapper.Map<HallDto>(createdHall);

                return CreatedAtAction(
                    nameof(GetHallById),
                    new { id = createdHall.ID },
                    new ApiResponse<HallDto>
                    {
                        StatusCode = 201,
                        Message = "Hall created successfully",
                        IsSuccess = true,
                        Data = hallDto
                    }
                );
            }
            catch (ArgumentException ex)
            {
                return Error<HallDto>(ex.Message, 400);
            }
            catch (Exception ex)
            {
                return Error<HallDto>($"Failed to create hall: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update an existing hall (HallManager only)
        /// </summary>
        /// <param name="id">Hall ID</param>
        /// <param name="updateHallDto">Updated hall data</param>
        /// <returns>Updated hall</returns>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<HallDto>>> UpdateHall(int id, [FromBody] HallUpdateDto updateHallDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<HallDto>($"Invalid data: {errors}", 400);
                }

                var existingHall = await _hallService.GetHallByIdAsync(id);
                if (existingHall == null)
                {
                    return Error<HallDto>($"Hall with ID {id} not found", 404);
                }

                // Map update DTO to entity, preserving the ID
                _mapper.Map(updateHallDto, existingHall);
                existingHall.ID = id;

                var updatedHall = await _hallService.UpdateHallAsync(existingHall);
                var hallDto = _mapper.Map<HallDto>(updatedHall);

                return Success(hallDto, "Hall updated successfully");
            }
            catch (ArgumentException ex)
            {
                return Error<HallDto>(ex.Message, 400);
            }
            catch (Exception ex)
            {
                return Error<HallDto>($"Failed to update hall: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update hall with file uploads (recommended - replaces base64)
        /// </summary>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpPut("{id:int}/files")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<HallDto>>> UpdateHallWithFiles(
            int id,
            [FromForm] HallUpdateWithFilesDto hallUpdateDto)
        {
            try
            {
                if (id != hallUpdateDto.ID)
                {
                    return Error<HallDto>("ID mismatch", 400);
                }

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<HallDto>($"Invalid data: {errors}", 400);
                }

                var hallEntity = _mapper.Map<HallApp.Core.Entities.ChamperEntities.Hall>(hallUpdateDto);
                
                // Build list of all image URLs (existing + new uploads)
                var allImageUrls = new List<string>();
                
                // Add existing images that user wants to keep
                if (hallUpdateDto.ExistingImageUrls != null && hallUpdateDto.ExistingImageUrls.Any())
                {
                    allImageUrls.AddRange(hallUpdateDto.ExistingImageUrls);
                }
                
                // Upload new images and get their URLs
                if (hallUpdateDto.NewImages != null && hallUpdateDto.NewImages.Any())
                {
                    var uploadedUrls = await _fileUploadService.SaveImagesAsync(
                        hallUpdateDto.NewImages.ToList(), 
                        "halls");
                    allImageUrls.AddRange(uploadedUrls);
                }
                
                // Create MediaFiles from all URLs
                hallEntity.MediaFiles = new List<HallApp.Core.Entities.ChamperEntities.MediaEntities.HallMedia>();
                int index = 0;
                foreach (var url in allImageUrls)
                {
                    hallEntity.MediaFiles.Add(new HallApp.Core.Entities.ChamperEntities.MediaEntities.HallMedia
                    {
                        URL = url,
                        HallID = hallEntity.ID,
                        MediaType = "image",
                        Gender = 3,
                        index = index++
                    });
                }
                
                // Update the hall
                var updatedHallEntity = await _hallService.UpdateHallAsync(hallEntity);
                
                if (updatedHallEntity == null)
                {
                    return Error<HallDto>("Couldn't update hall", 500);
                }
                
                // Handle manager assignments if provided
                if (hallUpdateDto.ManagerIds != null && hallUpdateDto.ManagerIds.Any())
                {
                    await _hallService.UpdateHallManagersAsync(id, hallUpdateDto.ManagerIds);
                }
                
                // Reload the hall with all relationships
                var reloadedHall = await _hallService.GetHallByIdAsync(id);
                var updatedHall = _mapper.Map<HallDto>(reloadedHall);
                return Success(updatedHall, "Hall updated successfully with file uploads");
            }
            catch (Exception ex)
            {
                return Error<HallDto>($"Failed to update hall: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Delete a hall (HallManager only)
        /// </summary>
        /// <param name="id">Hall ID</param>
        /// <returns>Success response</returns>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse>> DeleteHall(int id)
        {
            try
            {
                var existingHall = await _hallService.GetHallByIdAsync(id);
                if (existingHall == null)
                {
                    return Error($"Hall with ID {id} not found", 404);
                }

                var result = await _hallService.DeleteHallAsync(id);
                if (!result)
                {
                    return Error($"Failed to delete hall with ID {id}", 500);
                }

                return Success("Hall deleted successfully");
            }
            catch (Exception ex)
            {
                return Error($"Failed to delete hall: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Toggle hall active status (HallManager only)
        /// </summary>
        /// <param name="id">Hall ID</param>
        /// <param name="active">New active status</param>
        /// <returns>Updated hall</returns>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpPut("{id:int}/toggle-active")]
        public async Task<ActionResult<ApiResponse<HallDto>>> ToggleHallActive(int id, [FromQuery] bool active)
        {
            try
            {
                var existingHall = await _hallService.GetHallByIdAsync(id);
                if (existingHall == null)
                {
                    return Error<HallDto>($"Hall with ID {id} not found", 404);
                }

                existingHall.IsActive = active;
                var updatedHall = await _hallService.UpdateHallAsync(existingHall);
                var hallDto = _mapper.Map<HallDto>(updatedHall);

                return Success(hallDto, $"Hall {(active ? "activated" : "deactivated")} successfully");
            }
            catch (Exception ex)
            {
                return Error<HallDto>($"Failed to toggle hall status: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get halls for the current hall manager
        /// </summary>
        /// <returns>List of halls managed by the current user</returns>
        [Authorize(Roles = "HallManager")]
        [HttpGet("my-halls")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetMyHalls()
        {
            try
            {
                // Get the current user's ID from claims
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Error<IEnumerable<HallDto>>("User not authenticated", 401);
                }

                var halls = await _hallService.GetHallsByManagerAsync(userId);
                var hallDtos = _mapper.Map<List<HallDto>>(halls);

                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} halls for manager");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<HallDto>>($"Failed to get halls: {ex.Message}", 500);
            }
        }

        #endregion
    }
}
