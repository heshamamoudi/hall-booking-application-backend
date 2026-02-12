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
using System.Security.Claims;

namespace HallApp.Web.Controllers.Hall
{
    /// <summary>
    /// Public Hall browsing and HallManager CRUD operations.
    /// Query endpoints (GET) are public. Command endpoints (POST/PUT/DELETE) require HallManager/Admin role.
    /// </summary>
    [AllowAnonymous]
    [Route("api/halls")]
    public class HallController : BaseApiController
    {
        private readonly IHallService _hallService;
        private readonly IMapper _mapper;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<HallController> _logger;

        public HallController(
            IHallService hallService,
            IMapper mapper,
            IFileUploadService fileUploadService,
            ILogger<HallController> logger)
        {
            _hallService = hallService;
            _mapper = mapper;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // DUP-001 + DUP-004: Centralized hall ownership check
        private async Task<bool> UserOwnsHall(int hallId)
        {
            if (User.IsInRole("Admin")) return true;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return false;

            var managerHalls = await _hallService.GetHallsByManagerAsync(userId);
            return managerHalls.Any(h => h.ID == hallId);
        }

        // DUP-002: Extracted admin flag preservation helper
        private static void PreserveAdminOnlyFlags(
            HallApp.Core.Entities.ChamperEntities.Hall target,
            HallApp.Core.Entities.ChamperEntities.Hall source,
            bool isAdmin)
        {
            if (!isAdmin)
            {
                target.IsFeatured = source.IsFeatured;
                target.IsPremium = source.IsPremium;
                target.HasSpecialOffer = source.HasSpecialOffer;
            }
        }

        /// <summary>
        /// Get all halls for public browsing with pagination
        /// </summary>
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
                _logger.LogError(ex, "Error processing request for {Endpoint}", HttpContext.Request.Path);
                return StatusCode(500, new PaginatedApiResponse<HallDto>
                {
                    StatusCode = 500,
                    Message = "An error occurred processing your request. Please try again.",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Get hall by ID for public viewing
        /// </summary>
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
                _logger.LogError(ex, "Error retrieving hall {HallId}", id);
                return Error<HallDto>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Search halls by name or description
        /// </summary>
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
                _logger.LogError(ex, "Error searching halls");
                return Error<IEnumerable<HallDto>>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Get alternative halls available for the same date when one is rejected.
        /// PERF-002 FIX: Uses batch availability query instead of N+1 per-hall checks.
        /// </summary>
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

                // PERF-002: Single batch query replaces N+1 per-hall availability check
                var availableHalls = await _hallService.GetAvailableHallsForDateAsync(
                    eventDate, startDateTime, endDateTime, genderPreference);

                var hallDtos = _mapper.Map<List<HallDto>>(availableHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} alternative halls for {eventDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alternative halls");
                return Error<IEnumerable<HallDto>>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Get special offer halls (halls with active discounts/offers).
        /// PERF-001 FIX: Uses repository-level filtering instead of GetAllHallsAsync().
        /// </summary>
        [HttpGet("special-offers")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetSpecialOfferHalls([FromQuery] int limit = 6)
        {
            try
            {
                var specialOfferHalls = await _hallService.GetSpecialOfferHallsAsync(limit);
                var hallDtos = _mapper.Map<List<HallDto>>(specialOfferHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} special offer halls");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting special offer halls");
                return Error<IEnumerable<HallDto>>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Get featured halls.
        /// PERF-001 FIX: Uses repository-level filtering instead of GetAllHallsAsync().
        /// </summary>
        [HttpGet("featured")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetFeaturedHalls([FromQuery] int limit = 6)
        {
            try
            {
                var featuredHalls = await _hallService.GetFeaturedHallsAsync(limit);
                var hallDtos = _mapper.Map<List<HallDto>>(featuredHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} featured halls");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured halls");
                return Error<IEnumerable<HallDto>>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Get popular halls (high rating and multiple reviews).
        /// PERF-001 FIX: Uses repository-level filtering instead of GetAllHallsAsync().
        /// </summary>
        [HttpGet("popular")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetPopularHalls([FromQuery] int limit = 6)
        {
            try
            {
                var popularHalls = await _hallService.GetPopularHallsFilteredAsync(5, 3.8, limit);
                var hallDtos = _mapper.Map<List<HallDto>>(popularHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} popular halls");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular halls");
                return Error<IEnumerable<HallDto>>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Get premium halls.
        /// PERF-001 FIX: Uses repository-level filtering instead of GetAllHallsAsync().
        /// </summary>
        [HttpGet("premium")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetPremiumHalls([FromQuery] int limit = 6)
        {
            try
            {
                var premiumHalls = await _hallService.GetPremiumHallsAsync(limit);
                var hallDtos = _mapper.Map<List<HallDto>>(premiumHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} premium halls");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting premium halls");
                return Error<IEnumerable<HallDto>>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Get newly added halls (recently created halls).
        /// PERF-001 FIX: Uses repository-level filtering instead of GetAllHallsAsync().
        /// </summary>
        [HttpGet("newly-added")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetNewlyAddedHalls([FromQuery] int limit = 6)
        {
            try
            {
                var newlyAddedHalls = await _hallService.GetNewlyAddedHallsAsync(limit);
                var hallDtos = _mapper.Map<List<HallDto>>(newlyAddedHalls);
                return Success<IEnumerable<HallDto>>(hallDtos, $"Found {hallDtos.Count} newly added halls");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting newly added halls");
                return Error<IEnumerable<HallDto>>("An error occurred processing your request. Please try again.", 500);
            }
        }

        #region Hall Manager CRUD Operations

        /// <summary>
        /// Create a new hall (HallManager only)
        /// </summary>
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
                _logger.LogWarning(ex, "Validation error creating hall");
                return Error<HallDto>("Invalid hall data provided", 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request for {Endpoint}", HttpContext.Request.Path);
                return Error<HallDto>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Update an existing hall (HallManager only).
        /// DUP-002 FIX: Uses centralized admin flag preservation helper.
        /// MANAGER-FIX: Preserves existing managers unless explicitly updated by Admin.
        /// Validates at least 1 manager remains and prevents hall managers from removing themselves.
        /// </summary>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<HallDto>>> UpdateHall(int id, [FromBody] HallUpdateDto updateHallDto)
        {
            try
            {
                if (!await UserOwnsHall(id))
                {
                    return Error<HallDto>("You do not have permission to modify this hall", 403);
                }

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

                // Capture original flags before mapping overwrites them
                var originalFlags = (existingHall.IsFeatured, existingHall.IsPremium, existingHall.HasSpecialOffer);

                // AutoMapper ignores Managers for HallUpdateDto -> Hall mapping,
                // so existing managers on the entity are preserved through the mapping step.
                _mapper.Map(updateHallDto, existingHall);
                existingHall.ID = id;

                // DUP-002: Centralized admin flag preservation
                PreserveAdminOnlyFlags(existingHall,
                    new HallApp.Core.Entities.ChamperEntities.Hall
                    {
                        IsFeatured = originalFlags.IsFeatured,
                        IsPremium = originalFlags.IsPremium,
                        HasSpecialOffer = originalFlags.HasSpecialOffer
                    },
                    User.IsInRole("Admin"));

                // MANAGER-FIX: Explicitly null out Managers so UpdateHallAsync
                // does not attempt to reassign the tracked collection.
                // Managers are only modified through UpdateHallManagersAsync.
                existingHall.Managers = null;

                var updatedHall = await _hallService.UpdateHallAsync(existingHall);

                // MANAGER-FIX: Only allow manager updates if ManagerIds is explicitly provided
                // and contains at least one valid ID. Prevent self-removal for HallManagers.
                if (updateHallDto.ManagerIds != null && updateHallDto.ManagerIds.Any())
                {
                    // Validate: HallManagers cannot remove themselves
                    if (User.IsInRole("HallManager") && !User.IsInRole("Admin"))
                    {
                        var currentUserId = UserId;
                        var existingHallForManagerCheck = await _hallService.GetHallByIdAsync(id);
                        var currentManagerHallIds = existingHallForManagerCheck.Managers?
                            .Where(m => m.AppUserId == currentUserId)
                            .Select(m => m.Id)
                            .ToList() ?? new List<int>();

                        if (currentManagerHallIds.Any() && !updateHallDto.ManagerIds.Intersect(currentManagerHallIds).Any())
                        {
                            return Error<HallDto>("You cannot remove yourself as a manager of this hall. Another admin must perform this action.", 400);
                        }
                    }

                    await _hallService.UpdateHallManagersAsync(id, updateHallDto.ManagerIds);
                }

                var reloadedHall = await _hallService.GetHallByIdAsync(id);
                var hallDto = _mapper.Map<HallDto>(reloadedHall);

                return Success(hallDto, "Hall updated successfully");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating hall {HallId}", id);
                return Error<HallDto>(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request for {Endpoint}", HttpContext.Request.Path);
                return Error<HallDto>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Update hall with file uploads (recommended - replaces base64).
        /// DUP-002 FIX: Uses centralized admin flag preservation helper.
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
                if (!await UserOwnsHall(id))
                {
                    return Error<HallDto>("You do not have permission to modify this hall", 403);
                }

                if (id != hallUpdateDto.ID)
                {
                    return Error<HallDto>("ID mismatch", 400);
                }

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<HallDto>($"Invalid data: {errors}", 400);
                }

                var existingHallForFlags = await _hallService.GetHallByIdAsync(id);
                if (existingHallForFlags == null)
                {
                    return Error<HallDto>($"Hall with ID {id} not found", 404);
                }

                var hallEntity = _mapper.Map<HallApp.Core.Entities.ChamperEntities.Hall>(hallUpdateDto);

                // DUP-002: Centralized admin flag preservation
                PreserveAdminOnlyFlags(hallEntity, existingHallForFlags, User.IsInRole("Admin"));

                // Build list of all image URLs (existing + new uploads)
                var allImageUrls = new List<string>();

                if (hallUpdateDto.ExistingImageUrls != null && hallUpdateDto.ExistingImageUrls.Any())
                {
                    allImageUrls.AddRange(hallUpdateDto.ExistingImageUrls);
                }

                if (hallUpdateDto.NewImages != null && hallUpdateDto.NewImages.Any())
                {
                    var uploadedUrls = await _fileUploadService.SaveImagesAsync(
                        hallUpdateDto.NewImages.ToList(),
                        "halls");
                    allImageUrls.AddRange(uploadedUrls);
                }

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

                var updatedHallEntity = await _hallService.UpdateHallAsync(hallEntity);
                if (updatedHallEntity == null)
                {
                    return Error<HallDto>("Couldn't update hall", 500);
                }

                // MANAGER-FIX: Only allow manager updates if ManagerIds is explicitly provided
                // and contains at least one valid ID. Prevent self-removal for HallManagers.
                if (hallUpdateDto.ManagerIds != null && hallUpdateDto.ManagerIds.Any())
                {
                    // Validate: HallManagers cannot remove themselves
                    if (User.IsInRole("HallManager") && !User.IsInRole("Admin"))
                    {
                        var currentUserId = UserId;
                        var currentManagerHallIds = existingHallForFlags.Managers?
                            .Where(m => m.AppUserId == currentUserId)
                            .Select(m => m.Id)
                            .ToList() ?? new List<int>();

                        if (currentManagerHallIds.Any() && !hallUpdateDto.ManagerIds.Intersect(currentManagerHallIds).Any())
                        {
                            return Error<HallDto>("You cannot remove yourself as a manager of this hall. Another admin must perform this action.", 400);
                        }
                    }

                    await _hallService.UpdateHallManagersAsync(id, hallUpdateDto.ManagerIds);
                }

                var reloadedHall = await _hallService.GetHallByIdAsync(id);
                var updatedHall = _mapper.Map<HallDto>(reloadedHall);
                return Success(updatedHall, "Hall updated successfully with file uploads");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating hall {HallId}", id);
                return Error<HallDto>(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request for {Endpoint}", HttpContext.Request.Path);
                return Error<HallDto>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Delete a hall (HallManager only)
        /// </summary>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse>> DeleteHall(int id)
        {
            try
            {
                if (!await UserOwnsHall(id))
                {
                    return Error("You do not have permission to modify this hall", 403);
                }

                var existingHall = await _hallService.GetHallByIdAsync(id);
                if (existingHall == null)
                {
                    return Error($"Hall with ID {id} not found", 404);
                }

                var result = await _hallService.DeleteHallAsync(id);
                if (!result)
                {
                    return Error("Failed to delete hall", 500);
                }

                return Success("Hall deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request for {Endpoint}", HttpContext.Request.Path);
                return Error("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Toggle hall active status (HallManager only)
        /// </summary>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpPut("{id:int}/toggle-active")]
        public async Task<ActionResult<ApiResponse<HallDto>>> ToggleHallActive(int id, [FromQuery] bool active)
        {
            try
            {
                if (!await UserOwnsHall(id))
                {
                    return Error<HallDto>("You do not have permission to modify this hall", 403);
                }

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
                _logger.LogError(ex, "Error processing request for {Endpoint}", HttpContext.Request.Path);
                return Error<HallDto>("An error occurred processing your request. Please try again.", 500);
            }
        }

        /// <summary>
        /// Get halls for the current hall manager
        /// </summary>
        [Authorize(Roles = "HallManager")]
        [HttpGet("my-halls")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetMyHalls()
        {
            try
            {
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
                _logger.LogError(ex, "Error getting manager halls");
                return Error<IEnumerable<HallDto>>("An error occurred processing your request. Please try again.", 500);
            }
        }

        #endregion
    }
}
