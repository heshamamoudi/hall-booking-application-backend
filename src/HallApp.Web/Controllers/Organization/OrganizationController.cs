using HallApp.Application.DTOs.Organization;
using HallApp.Core.Entities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HallEntity = HallApp.Core.Entities.ChamperEntities.Hall;
using VendorEntity = HallApp.Core.Entities.VendorEntities.Vendor;
using AutoMapper;
using HallApp.Application.DTOs.Halls.Hall;
using HallApp.Application.Services;

namespace HallApp.Web.Controllers.Organization;

/// <summary>
/// Organization management controller.
/// Handles organization CRUD, team member management, and organization resource listing.
/// </summary>
[Route("api/organizations")]
public class OrganizationController : BaseApiController
{
    private readonly IOrganizationService _organizationService;
    private readonly ITeamMemberService _teamMemberService;
    private readonly IHallService _hallService;
    private readonly IVendorService _vendorService;
    private readonly IHallStatsService _hallStatsService;
    private readonly IMapper _mapper;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(
        IOrganizationService organizationService,
        ITeamMemberService teamMemberService,
        IHallService hallService,
        IVendorService vendorService,
        IHallStatsService hallStatsService,
        IMapper mapper,
        ILogger<OrganizationController> logger)
    {
        _organizationService = organizationService;
        _teamMemberService = teamMemberService;
        _hallService = hallService;
        _vendorService = vendorService;
        _hallStatsService = hallStatsService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get the current user's organization (owner or member).
    /// </summary>
    [Authorize(Roles = "HallOrganizationManager,VendorOrganizationManager,HallManager,VendorManager")]
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<OrganizationDto>>> GetMyOrganization()
    {
        try
        {
            var organization = await _organizationService.GetOrganizationByOwnerId(UserId);
            if (organization == null)
            {
                return Ok(new ApiResponse<OrganizationDto>
                {
                    StatusCode = 404,
                    Message = "No organization found for this user",
                    IsSuccess = false
                });
            }

            int resourceCount = await GetResourceCount(organization);
            var dto = MapToDto(organization, resourceCount);

            return Success(dto, "Organization retrieved successfully");
        }
        catch (Exception ex)
        {
            return Error<OrganizationDto>($"Failed to retrieve organization: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get organization by ID (members only).
    /// </summary>
    [Authorize(Roles = "HallOrganizationManager,VendorOrganizationManager,HallManager,VendorManager")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<OrganizationDto>>> GetOrganizationById(int id)
    {
        try
        {
            var organization = await _organizationService.GetOrganizationById(id);
            if (organization == null)
            {
                return Error<OrganizationDto>("Organization not found", 404);
            }

            // Verify user is a member of this organization
            var isMember = organization.Members?.Any(m => m.AppUserId == UserId) ?? false;
            var isOwner = organization.OwnerId == UserId;

            if (!isMember && !isOwner && !User.IsInRole("Admin"))
            {
                return Error<OrganizationDto>("You do not have access to this organization", 403);
            }

            int resourceCount = await GetResourceCount(organization);
            var dto = MapToDto(organization, resourceCount);

            return Success(dto, "Organization retrieved successfully");
        }
        catch (Exception ex)
        {
            return Error<OrganizationDto>($"Failed to retrieve organization: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get all organizations (Admin only).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<OrganizationDto>>>> GetAllOrganizations()
    {
        try
        {
            var organizations = await _organizationService.GetAllOrganizations();
            var dtos = organizations.Select(o => MapToDto(o)).ToList();

            return Success(dtos, "Organizations retrieved successfully");
        }
        catch (Exception ex)
        {
            return Error<List<OrganizationDto>>($"Failed to retrieve organizations: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new organization (Admin only, used when setting up a new organization manager).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrganizationDto>>> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        try
        {
            var businessInfo = MapRequestToEntity(request);
            var organization = await _organizationService.CreateOrganization(
                request.Name, request.Type, request.OwnerId, businessInfo);

            var dto = MapToDto(organization);
            dto.MemberCount = 1; // Owner is always added as a member

            return Success(dto, "Organization created successfully");
        }
        catch (Exception ex)
        {
            return Error<OrganizationDto>($"Failed to create organization: {ex.Message}", 400);
        }
    }

    /// <summary>
    /// Update organization details including business registration, address, contact, and bank info.
    /// Only the organization owner or an admin can update.
    /// </summary>
    [Authorize(Roles = "HallOrganizationManager,VendorOrganizationManager,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<OrganizationDto>>> UpdateOrganization(int id, [FromBody] UpdateOrganizationDto dto)
    {
        try
        {
            var organization = await _organizationService.GetOrganizationById(id);
            if (organization == null)
            {
                return Error<OrganizationDto>("Organization not found", 404);
            }

            // Only the owner or an admin can update
            if (organization.OwnerId != UserId && !User.IsInRole("Admin"))
            {
                return Error<OrganizationDto>("Only the organization owner can update organization details", 403);
            }

            var updatedFields = MapDtoToEntity(dto);
            var updated = await _organizationService.UpdateOrganization(id, updatedFields);

            // Reload with navigation properties for a complete DTO
            var reloaded = await _organizationService.GetOrganizationById(id);
            int resourceCount = await GetResourceCount(reloaded);
            var result = MapToDto(reloaded, resourceCount);

            return Success(result, "Organization updated successfully");
        }
        catch (ArgumentException ex)
        {
            return Error<OrganizationDto>(ex.Message, 404);
        }
        catch (Exception ex)
        {
            return Error<OrganizationDto>($"Failed to update organization: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get team members for an organization.
    /// </summary>
    [Authorize(Roles = "HallOrganizationManager,VendorOrganizationManager,HallManager,VendorManager")]
    [HttpGet("{id:int}/members")]
    public async Task<ActionResult<ApiResponse<List<TeamMemberDto>>>> GetMembers(int id)
    {
        try
        {
            // Verify user belongs to this organization
            var org = await _organizationService.GetOrganizationById(id);
            if (org == null)
            {
                return Error<List<TeamMemberDto>>("Organization not found", 404);
            }

            if (!IsAdmin && org.OwnerId != UserId &&
                (org.Members == null || !org.Members.Any(m => m.AppUserId == UserId)))
            {
                return Error<List<TeamMemberDto>>("You do not have access to this organization", 403);
            }

            var members = await _teamMemberService.GetTeamMembers(id);

            // HIGH-6 FIX: Load all organization resources ONCE before the loop
            // instead of making 2 database calls per member (N+1 query elimination).
            // This reduces queries from 2*N to a fixed 3 regardless of member count.
            var memberAppUserIds = members.Select(m => m.AppUserId).ToList();

            // Pre-load organization-scoped resources and manager ID mappings
            Dictionary<int, int>? appUserToManagerIdMap = null;
            List<HallEntity>? orgHalls = null;
            List<VendorEntity>? orgVendors = null;

            if (org.Type == "HallManagement")
            {
                orgHalls = await _hallService.GetOrganizationHallsAsync(org.Id);
                appUserToManagerIdMap = await _hallService.GetAppUserToHallManagerIdMapAsync(memberAppUserIds);
            }
            else if (org.Type == "VendorManagement")
            {
                orgVendors = await _vendorService.GetOrganizationVendorsAsync(org.Id);
                appUserToManagerIdMap = await _vendorService.GetAppUserToVendorManagerIdMapAsync(memberAppUserIds);
            }

            // Build TeamMemberDto list with assigned resources using in-memory filtering
            var dtos = new List<TeamMemberDto>();
            foreach (var member in members)
            {
                var memberDto = new TeamMemberDto
                {
                    Id = member.Id,
                    UserId = member.AppUserId,
                    FullName = member.AppUser != null
                        ? $"{member.AppUser.FirstName} {member.AppUser.LastName}".Trim()
                        : string.Empty,
                    Email = member.AppUser?.Email ?? string.Empty,
                    Role = member.Role,
                    JoinedAt = member.JoinedAt,
                    CanManageTeam = member.CanManageTeam
                };

                // Filter pre-loaded resources in memory by manager ID
                if (org.Type == "HallManagement" && orgHalls != null && appUserToManagerIdMap != null)
                {
                    if (appUserToManagerIdMap.TryGetValue(member.AppUserId, out var hallManagerId))
                    {
                        memberDto.AssignedHalls = orgHalls
                            .Where(h => h.AssignedToHallManagerId == hallManagerId)
                            .Select(h => new AssignedResourceDto
                            {
                                Id = h.ID,
                                Name = h.Name
                            }).ToList();
                    }
                    else
                    {
                        memberDto.AssignedHalls = [];
                    }
                }
                else if (org.Type == "VendorManagement" && orgVendors != null && appUserToManagerIdMap != null)
                {
                    if (appUserToManagerIdMap.TryGetValue(member.AppUserId, out var vendorManagerId))
                    {
                        memberDto.AssignedVendors = orgVendors
                            .Where(v => v.AssignedToVendorManagerId == vendorManagerId)
                            .Select(v => new AssignedResourceDto
                            {
                                Id = v.Id,
                                Name = v.Name
                            }).ToList();
                    }
                    else
                    {
                        memberDto.AssignedVendors = [];
                    }
                }

                dtos.Add(memberDto);
            }

            return Success(dtos, $"Found {dtos.Count} team members");
        }
        catch (Exception ex)
        {
            return Error<List<TeamMemberDto>>($"Failed to retrieve team members: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Add a new team member to the organization. Only the organization owner can add members.
    /// </summary>
    [Authorize(Roles = "HallOrganizationManager,VendorOrganizationManager")]
    [HttpPost("{id:int}/members")]
    public async Task<ActionResult<ApiResponse<TeamMemberDto>>> AddMember(int id, [FromBody] AddTeamMemberDto dto)
    {
        try
        {
            // Only organization owner can add members
            var canManage = await _teamMemberService.CanManageTeam(UserId, id);
            if (!canManage)
            {
                return Error<TeamMemberDto>("Only the organization owner can add team members", 403);
            }

            var member = await _teamMemberService.AddTeamMember(
                id, dto.Email, dto.FullName, dto.Role, dto.PasswordOption, dto.Password, UserId);

            // Reload with AppUser data
            var members = await _teamMemberService.GetTeamMembers(id);
            var addedMember = members.FirstOrDefault(m => m.Id == member.Id);

            var result = new TeamMemberDto
            {
                Id = member.Id,
                UserId = member.AppUserId,
                FullName = addedMember?.AppUser != null
                    ? $"{addedMember.AppUser.FirstName} {addedMember.AppUser.LastName}".Trim()
                    : dto.FullName,
                Email = addedMember?.AppUser?.Email ?? dto.Email,
                Role = member.Role,
                JoinedAt = member.JoinedAt,
                CanManageTeam = member.CanManageTeam
            };

            return Success(result, "Team member added successfully");
        }
        catch (InvalidOperationException ex)
        {
            return Error<TeamMemberDto>(ex.Message, 409);
        }
        catch (ArgumentException ex)
        {
            return Error<TeamMemberDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return Error<TeamMemberDto>($"Failed to add team member: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update a team member's role. Only the organization owner can update roles.
    /// </summary>
    [Authorize(Roles = "HallOrganizationManager,VendorOrganizationManager")]
    [HttpPut("{id:int}/members/{userId:int}/role")]
    public async Task<ActionResult<ApiResponse>> UpdateMemberRole(int id, int userId, [FromBody] UpdateMemberRoleDto dto)
    {
        try
        {
            // Only organization owner can update roles
            var canManage = await _teamMemberService.CanManageTeam(UserId, id);
            if (!canManage)
            {
                return Error("Only the organization owner can update member roles", 403);
            }

            // Find the member
            var members = await _teamMemberService.GetTeamMembers(id);
            var member = members.FirstOrDefault(m => m.AppUserId == userId);
            if (member == null)
            {
                return Error("Team member not found in this organization", 404);
            }

            // Cannot change owner role
            if (member.Role == "Owner")
            {
                return Error("Cannot change the owner's role", 400);
            }

            // Update the role (note: this updates the OrganizationMember role, not the Identity role)
            await _teamMemberService.UpdateMemberRole(id, userId, dto.NewRole);

            return Success("Member role updated successfully");
        }
        catch (Exception ex)
        {
            return Error($"Failed to update member role: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Remove a team member from the organization. Only the organization owner can remove members.
    /// </summary>
    [Authorize(Roles = "HallOrganizationManager,VendorOrganizationManager")]
    [HttpDelete("{id:int}/members/{userId:int}")]
    public async Task<ActionResult<ApiResponse>> RemoveMember(int id, int userId)
    {
        try
        {
            // Only organization owner can remove members
            var canManage = await _teamMemberService.CanManageTeam(UserId, id);
            if (!canManage)
            {
                return Error("Only the organization owner can remove team members", 403);
            }

            var result = await _teamMemberService.RemoveTeamMember(id, userId);
            if (!result)
            {
                return Error("Team member not found in this organization", 404);
            }

            return Success("Team member removed successfully");
        }
        catch (InvalidOperationException ex)
        {
            return Error(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return Error($"Failed to remove team member: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get halls belonging to an organization with statistics.
    /// Only members, the owner, or admins can view an organization's halls.
    /// </summary>
    [Authorize(Roles = "HallOrganizationManager,HallManager")]
    [HttpGet("{id:int}/halls")]
    public async Task<ActionResult<ApiResponse<List<HallWithStatsDto>>>> GetOrganizationHalls(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var org = await _organizationService.GetOrganizationById(id);
            if (org == null)
            {
                return Error<List<HallWithStatsDto>>("Organization not found", 404);
            }

            var isMember = org.Members?.Any(m => m.AppUserId == UserId) ?? false;
            if (!isMember && org.OwnerId != UserId && !User.IsInRole("Admin"))
            {
                return Error<List<HallWithStatsDto>>("You do not have access to this organization", 403);
            }

            var halls = await _hallService.GetOrganizationHallsAsync(id);
            if (!halls.Any())
            {
                return Success<List<HallWithStatsDto>>(
                    new List<HallWithStatsDto>(), "No halls found for organization");
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

            return Success(hallDtos, $"Found {hallDtos.Count} halls with statistics");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Organization halls request cancelled for organization {OrgId}", id);
            return StatusCode(499, new ApiResponse<List<HallWithStatsDto>>
            {
                StatusCode = 499,
                Message = "Request cancelled",
                IsSuccess = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting halls for organization {OrgId}", id);
            return Error<List<HallWithStatsDto>>(
                "An error occurred processing your request. Please try again.", 500);
        }
    }

    /// <summary>
    /// Get vendors belonging to an organization.
    /// Only members, the owner, or admins can view an organization's vendors.
    /// </summary>
    [Authorize(Roles = "VendorOrganizationManager,VendorManager")]
    [HttpGet("{id:int}/vendors")]
    public async Task<ActionResult<ApiResponse<List<AssignedResourceDto>>>> GetOrganizationVendors(int id)
    {
        try
        {
            var org = await _organizationService.GetOrganizationById(id);
            if (org == null)
            {
                return Error<List<AssignedResourceDto>>("Organization not found", 404);
            }

            var isMember = org.Members?.Any(m => m.AppUserId == UserId) ?? false;
            if (!isMember && org.OwnerId != UserId && !User.IsInRole("Admin"))
            {
                return Error<List<AssignedResourceDto>>("You do not have access to this organization", 403);
            }

            var vendors = await _vendorService.GetOrganizationVendorsAsync(id);
            var dtos = vendors.Select(v => new AssignedResourceDto
            {
                Id = v.Id,
                Name = v.Name
            }).ToList();

            return Success(dtos, $"Found {dtos.Count} vendors");
        }
        catch (Exception ex)
        {
            return Error<List<AssignedResourceDto>>($"Failed to retrieve organization vendors: {ex.Message}", 500);
        }
    }

    // ===================================================================
    // Private Helper Methods
    // ===================================================================

    /// <summary>
    /// Maps an Organization entity to an OrganizationDto including all business fields.
    /// </summary>
    private static OrganizationDto MapToDto(Core.Entities.Organization org, int resourceCount = 0)
    {
        return new OrganizationDto
        {
            Id = org.Id,
            Name = org.Name,
            Type = org.Type,
            OwnerId = org.OwnerId,
            OwnerName = org.Owner != null
                ? $"{org.Owner.FirstName} {org.Owner.LastName}".Trim()
                : string.Empty,
            MemberCount = org.Members?.Count ?? 0,
            ResourceCount = resourceCount,
            CreatedAt = org.CreatedAt,
            IsActive = org.IsActive,

            // Business Registration
            CommercialRegistrationNumber = org.CommercialRegistrationNumber,
            VatNumber = org.VatNumber,
            LegalName = org.LegalName,

            // Legal Address
            BuildingNumber = org.BuildingNumber,
            StreetName = org.StreetName,
            District = org.District,
            City = org.City,
            PostalCode = org.PostalCode,
            CountryCode = org.CountryCode,

            // Contact Info
            Email = org.Email,
            Phone = org.Phone,
            WhatsApp = org.WhatsApp,
            Website = org.Website,

            // Bank Account
            BankName = org.BankName,
            BankAccountNumber = org.BankAccountNumber,
            BankIban = org.BankIban,

            // Metadata
            UpdatedAt = org.UpdatedAt,
            IsVerified = org.IsVerified,
            VerifiedAt = org.VerifiedAt
        };
    }

    /// <summary>
    /// Maps an UpdateOrganizationDto to an Organization entity for the service layer.
    /// </summary>
    private static Core.Entities.Organization MapDtoToEntity(UpdateOrganizationDto dto)
    {
        return new Core.Entities.Organization
        {
            Name = dto.Name,
            CommercialRegistrationNumber = dto.CommercialRegistrationNumber,
            VatNumber = dto.VatNumber,
            LegalName = dto.LegalName,
            BuildingNumber = dto.BuildingNumber,
            StreetName = dto.StreetName,
            District = dto.District,
            City = dto.City,
            PostalCode = dto.PostalCode,
            CountryCode = dto.CountryCode,
            Email = dto.Email,
            Phone = dto.Phone,
            WhatsApp = dto.WhatsApp,
            Website = dto.Website,
            BankName = dto.BankName,
            BankAccountNumber = dto.BankAccountNumber,
            BankIban = dto.BankIban
        };
    }

    /// <summary>
    /// Maps a CreateOrganizationRequest to an Organization entity for business fields.
    /// </summary>
    private static Core.Entities.Organization MapRequestToEntity(CreateOrganizationRequest request)
    {
        return new Core.Entities.Organization
        {
            CommercialRegistrationNumber = request.CommercialRegistrationNumber,
            VatNumber = request.VatNumber,
            LegalName = request.LegalName,
            BuildingNumber = request.BuildingNumber,
            StreetName = request.StreetName,
            District = request.District,
            City = request.City,
            PostalCode = request.PostalCode,
            CountryCode = request.CountryCode,
            Email = request.Email,
            Phone = request.Phone,
            WhatsApp = request.WhatsApp,
            Website = request.Website,
            BankName = request.BankName,
            BankAccountNumber = request.BankAccountNumber,
            BankIban = request.BankIban
        };
    }

    /// <summary>
    /// Gets the resource count (halls or vendors) for an organization.
    /// </summary>
    private async Task<int> GetResourceCount(Core.Entities.Organization organization)
    {
        if (organization.Type == "HallManagement")
        {
            var halls = await _hallService.GetOrganizationHallsAsync(organization.Id);
            return halls.Count;
        }

        if (organization.Type == "VendorManagement")
        {
            var vendors = await _vendorService.GetOrganizationVendorsAsync(organization.Id);
            return vendors.Count;
        }

        return 0;
    }
}

/// <summary>
/// Request DTO for creating an organization (Admin-only).
/// Includes optional business registration fields that can be populated at creation time.
/// </summary>
public class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int OwnerId { get; set; }

    // Optional business registration fields
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;

    // Optional legal address
    public string BuildingNumber { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;

    // Optional contact info
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // Optional bank account
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankIban { get; set; } = string.Empty;
}
