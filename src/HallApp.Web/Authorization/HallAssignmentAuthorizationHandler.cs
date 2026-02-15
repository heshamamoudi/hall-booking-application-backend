using System.Security.Claims;
using HallApp.Core.Constants;
using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace HallApp.Web.Authorization;

/// <summary>
/// Authorization handler that enforces hall assignment checks for HallManager role.
///
/// Access rules:
/// - Admin: Always allowed (bypasses check)
/// - OrganizationManager: Always allowed (owns the organization)
/// - HallManager: Only allowed if the requested hall is assigned to them
///
/// The handler reads the hallId from the route data (e.g., /api/halls/{hallId}).
/// If no hallId is present in the route, the handler succeeds (the controller
/// should handle data filtering at the service layer).
/// </summary>
public class HallAssignmentAuthorizationHandler : AuthorizationHandler<HallAssignmentRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHallManagerService _hallManagerService;
    private readonly ILogger<HallAssignmentAuthorizationHandler> _logger;

    public HallAssignmentAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        IHallManagerService hallManagerService,
        ILogger<HallAssignmentAuthorizationHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _hallManagerService = hallManagerService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HallAssignmentRequirement requirement)
    {
        var user = context.User;

        // Admin and HallOrganizationManager bypass assignment checks
        if (user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.HallOrganizationManager))
        {
            context.Succeed(requirement);
            return;
        }

        // HallManager must have the hall assigned to them
        if (!user.IsInRole(AppRoles.HallManager))
        {
            context.Fail();
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return;
        }

        // Try to extract hallId from route values
        var hallIdString = httpContext.Request.RouteValues["hallId"]?.ToString()
                        ?? httpContext.Request.RouteValues["id"]?.ToString();

        // If no hall ID in route, succeed and let the controller filter data
        if (string.IsNullOrEmpty(hallIdString) || !int.TryParse(hallIdString, out var hallId))
        {
            context.Succeed(requirement);
            return;
        }

        // Get the user's ID from claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("HallAssignment check failed: no valid user ID in claims");
            context.Fail();
            return;
        }

        // Check if this hall is assigned to the HallManager
        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userId);
        if (hallManager?.Halls?.Any(h => h.ID == hallId) == true)
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "HallAssignment check failed: HallManager {UserId} attempted to access hall {HallId} which is not assigned to them",
            userId, hallId);
        context.Fail();
    }
}
