using HallApp.Application.DTOs.Organization;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Organization;

/// <summary>
/// Vendor assignment controller.
/// Handles assigning vendors to specific VendorManagers within an organization.
/// </summary>
[Route("api/vendors")]
public class VendorAssignmentController : BaseApiController
{
    private readonly IVendorService _vendorService;
    private readonly IOrganizationService _organizationService;

    public VendorAssignmentController(
        IVendorService vendorService,
        IOrganizationService organizationService)
    {
        _vendorService = vendorService;
        _organizationService = organizationService;
    }

    /// <summary>
    /// Assign a vendor to a specific VendorManager.
    /// Only the organization owner (VendorOrganizationManager) can assign vendors.
    /// The vendor must belong to the caller's organization, and the target manager must be a member.
    /// Cannot reassign a vendor that is already assigned.
    /// </summary>
    [Authorize(Roles = "VendorOrganizationManager")]
    [HttpPost("{id:int}/assign")]
    public async Task<ActionResult<ApiResponse>> AssignVendorToManager(int id, [FromBody] AssignResourceDto dto)
    {
        try
        {
            if (dto.ManagerId <= 0)
            {
                return Error("Invalid manager ID", 400);
            }

            // 1. Verify the vendor exists and belongs to an organization
            var vendor = await _vendorService.GetVendorByIdAsync(id);
            if (vendor == null)
            {
                return Error("Vendor not found", 404);
            }

            if (vendor.OrganizationId == null)
            {
                return Error("This vendor is not associated with any organization", 400);
            }

            // 2. Verify the caller is the owner of the organization that owns this vendor
            var org = await _organizationService.GetOrganizationById(vendor.OrganizationId.Value);
            if (org == null)
            {
                return Error("Organization not found", 404);
            }

            if (org.OwnerId != UserId && !User.IsInRole("Admin"))
            {
                return Error("Only the organization owner can assign vendors", 403);
            }

            // 3. Verify the target manager is a member of the same organization
            var isTargetMember = org.Members?.Any(m => m.AppUserId == dto.ManagerId) ?? false;
            if (!isTargetMember)
            {
                return Error("The target manager is not a member of this organization", 403);
            }

            var result = await _vendorService.AssignVendorToManagerAsync(id, dto.ManagerId);
            if (!result)
            {
                return Error("Vendor not found", 404);
            }

            return Success("Vendor assigned to manager successfully");
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
            return Error($"Failed to assign vendor: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get vendors assigned to the currently authenticated VendorManager.
    /// </summary>
    [Authorize(Roles = "VendorOrganizationManager,VendorManager")]
    [HttpGet("my-assigned")]
    public async Task<ActionResult<ApiResponse<List<AssignedResourceDto>>>> GetMyAssignedVendors()
    {
        try
        {
            var vendors = await _vendorService.GetManagerAssignedVendorsAsync(UserId);
            var dtos = vendors.Select(v => new AssignedResourceDto
            {
                Id = v.Id,
                Name = v.Name
            }).ToList();

            return Success(dtos, $"Found {dtos.Count} assigned vendors");
        }
        catch (Exception ex)
        {
            return Error<List<AssignedResourceDto>>($"Failed to retrieve assigned vendors: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get all vendors for a specific organization.
    /// Only members, the owner, or admins can view an organization's vendors.
    /// </summary>
    [Authorize(Roles = "VendorOrganizationManager,VendorManager")]
    [HttpGet("organization/{orgId:int}")]
    public async Task<ActionResult<ApiResponse<List<AssignedResourceDto>>>> GetVendorsByOrganization(int orgId)
    {
        try
        {
            var org = await _organizationService.GetOrganizationById(orgId);
            if (org == null)
            {
                return Error<List<AssignedResourceDto>>("Organization not found", 404);
            }

            var isMember = org.Members?.Any(m => m.AppUserId == UserId) ?? false;
            if (!isMember && org.OwnerId != UserId && !User.IsInRole("Admin"))
            {
                return Error<List<AssignedResourceDto>>("You do not have access to this organization", 403);
            }

            var vendors = await _vendorService.GetOrganizationVendorsAsync(orgId);
            var dtos = vendors.Select(v => new AssignedResourceDto
            {
                Id = v.Id,
                Name = v.Name
            }).ToList();

            return Success(dtos, $"Found {dtos.Count} vendors in organization");
        }
        catch (Exception ex)
        {
            return Error<List<AssignedResourceDto>>($"Failed to retrieve organization vendors: {ex.Message}", 500);
        }
    }
}
