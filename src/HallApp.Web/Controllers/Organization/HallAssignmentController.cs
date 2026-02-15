using HallApp.Application.DTOs.Organization;
using HallApp.Application.DTOs.Halls.Hall;
using HallApp.Application.Services;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;

namespace HallApp.Web.Controllers.Organization;

/// <summary>
/// Hall assignment controller.
/// Handles assigning halls to specific HallManagers within an organization.
/// </summary>
[Route("api/halls")]
public class HallAssignmentController : BaseApiController
{
    private readonly IHallService _hallService;
    private readonly IOrganizationService _organizationService;
    private readonly IHallStatsService _hallStatsService;
    private readonly IMapper _mapper;
    private readonly ILogger<HallAssignmentController> _logger;

    public HallAssignmentController(
        IHallService hallService,
        IOrganizationService organizationService,
        IHallStatsService hallStatsService,
        IMapper mapper,
        ILogger<HallAssignmentController> logger)
    {
        _hallService = hallService;
        _organizationService = organizationService;
        _hallStatsService = hallStatsService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Assign a hall to a specific HallManager.
    /// Only the organization owner (HallOrganizationManager) can assign halls.
    /// The hall must belong to the caller's organization, and the target manager must be a member.
    /// Cannot reassign a hall that is already assigned.
    /// </summary>
    [Authorize(Roles = "HallOrganizationManager")]
    [HttpPost("{id:int}/assign")]
    public async Task<ActionResult<ApiResponse>> AssignHallToManager(int id, [FromBody] AssignResourceDto dto)
    {
        try
        {
            if (dto.ManagerId <= 0)
            {
                return Error("Invalid manager ID", 400);
            }

            // 1. Verify the hall exists and belongs to an organization
            var hall = await _hallService.GetHallByIdAsync(id);
            if (hall == null)
            {
                return Error("Hall not found", 404);
            }

            if (hall.OrganizationId == null)
            {
                return Error("This hall is not associated with any organization", 400);
            }

            // 2. Verify the caller is the owner of the organization that owns this hall
            var org = await _organizationService.GetOrganizationById(hall.OrganizationId.Value);
            if (org == null)
            {
                return Error("Organization not found", 404);
            }

            if (org.OwnerId != UserId && !User.IsInRole("Admin"))
            {
                return Error("Only the organization owner can assign halls", 403);
            }

            // 3. Verify the target manager is a member of the same organization
            var isTargetMember = org.Members?.Any(m => m.AppUserId == dto.ManagerId) ?? false;
            if (!isTargetMember)
            {
                return Error("The target manager is not a member of this organization", 403);
            }

            var result = await _hallService.AssignHallToManagerAsync(id, dto.ManagerId);
            if (!result)
            {
                return Error("Hall not found", 404);
            }

            return Success("Hall assigned to manager successfully");
        }
        catch (InvalidOperationException ex)
        {
            return Error(ex.Message, 409);
        }
        catch (ArgumentException ex)
        {
            return Error(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return Error($"Failed to assign hall: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get halls assigned to the currently authenticated HallManager.
    /// </summary>
    [Authorize(Roles = "HallOrganizationManager,HallManager")]
    [HttpGet("my-assigned")]
    public async Task<ActionResult<ApiResponse<List<AssignedResourceDto>>>> GetMyAssignedHalls()
    {
        try
        {
            var halls = await _hallService.GetManagerAssignedHallsAsync(UserId);
            var dtos = halls.Select(h => new AssignedResourceDto
            {
                Id = h.ID,
                Name = h.Name
            }).ToList();

            return Success(dtos, $"Found {dtos.Count} assigned halls");
        }
        catch (Exception ex)
        {
            return Error<List<AssignedResourceDto>>($"Failed to retrieve assigned halls: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get all halls for a specific organization with statistics.
    /// Only members, the owner, or admins can view an organization's halls.
    /// </summary>
    [Authorize(Roles = "HallOrganizationManager,HallManager")]
    [HttpGet("organization/{orgId:int}")]
    public async Task<ActionResult<ApiResponse<List<HallWithStatsDto>>>> GetHallsByOrganization(int orgId, CancellationToken cancellationToken = default)
    {
        try
        {
            var org = await _organizationService.GetOrganizationById(orgId);
            if (org == null)
            {
                return Error<List<HallWithStatsDto>>("Organization not found", 404);
            }

            var isMember = org.Members?.Any(m => m.AppUserId == UserId) ?? false;
            if (!isMember && org.OwnerId != UserId && !User.IsInRole("Admin"))
            {
                return Error<List<HallWithStatsDto>>("You do not have access to this organization", 403);
            }

            var halls = await _hallService.GetOrganizationHallsAsync(orgId);

            if (!halls.Any())
            {
                return Success<List<HallWithStatsDto>>(
                    new List<HallWithStatsDto>(), "No halls found in organization");
            }

            // Map to HallWithStatsDto
            var hallDtos = _mapper.Map<List<HallWithStatsDto>>(halls);

            // Batch-load stats for all halls
            var hallIds = halls.Select(h => h.ID).ToList();
            var statsMap = await _hallStatsService.GetStatsForHallsAsync(hallIds, cancellationToken);

            // Enrich DTOs with stats
            foreach (var dto in hallDtos)
            {
                if (statsMap.TryGetValue(dto.ID, out var stats))
                {
                    dto.TotalBookings = stats.TotalBookings;
                    dto.TotalRevenue = stats.TotalRevenue;
                    dto.PaidInvoices = stats.PaidInvoices;
                    dto.PendingInvoices = stats.PendingInvoices;
                    dto.TotalReviews = stats.TotalReviews;
                }
            }

            return Success(hallDtos, $"Found {hallDtos.Count} halls in organization");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve organization halls for org {OrgId}", orgId);
            return Error<List<HallWithStatsDto>>($"Failed to retrieve organization halls: {ex.Message}", 500);
        }
    }
}
