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
    [Route("api/v1/halls")]
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
    }
}
