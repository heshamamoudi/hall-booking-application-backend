using HallApp.Core.Entities;
using HallApp.Core.Interfaces.IServices;
using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HallApp.Infrastructure.Services;

/// <summary>
/// Service for organization CRUD operations.
/// Organizations group halls or vendors under a single management entity.
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly DataContext _context;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(DataContext context, ILogger<OrganizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Organization> CreateOrganization(string name, string type, int ownerId)
    {
        return await CreateOrganization(name, type, ownerId, null);
    }

    public async Task<Organization> CreateOrganization(string name, string type, int ownerId, Organization? businessInfo)
    {
        // Validate type
        if (type != "HallManagement" && type != "VendorManagement")
        {
            throw new ArgumentException("Organization type must be 'HallManagement' or 'VendorManagement'.");
        }

        // Application-level check (fast path for non-concurrent requests).
        // The unique filtered index IX_Organizations_OwnerId_UniqueActive enforces
        // this at the database level to prevent the race condition.
        var existingOrg = await _context.Organizations
            .FirstOrDefaultAsync(o => o.OwnerId == ownerId && o.IsActive);

        if (existingOrg != null)
        {
            throw new InvalidOperationException("User already owns an active organization.");
        }

        // Wrap organization + owner membership creation in a transaction
        // so both succeed or both fail atomically.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var organization = new Organization
            {
                Name = name,
                Type = type,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Apply business info fields if provided
            if (businessInfo != null)
            {
                ApplyBusinessFields(organization, businessInfo);
            }

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Add owner as a member with Owner role
            var ownerMember = new OrganizationMember
            {
                OrganizationId = organization.Id,
                AppUserId = ownerId,
                Role = "Owner",
                CanManageTeam = true,
                CanCreateResources = true,
                JoinedAt = DateTime.UtcNow
            };

            _context.OrganizationMembers.Add(ownerMember);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Organization '{Name}' created by user {OwnerId} with type {Type}", name, ownerId, type);

            return organization;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // A concurrent request created the organization between our check and insert.
            // The unique filtered index IX_Organizations_OwnerId_UniqueActive caught it.
            await transaction.RollbackAsync();
            _logger.LogWarning(
                "Duplicate organization creation attempt by user {OwnerId} blocked by unique constraint",
                ownerId);
            throw new InvalidOperationException("User already owns an active organization.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "Failed to create organization '{Name}' for user {OwnerId}. Transaction rolled back",
                name, ownerId);
            throw;
        }
    }

    /// <summary>
    /// Determines whether a DbUpdateException was caused by a unique constraint violation.
    /// Handles PostgreSQL error code 23505 (unique_violation).
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505";
    }

    public async Task<Organization?> GetOrganizationById(int id)
    {
        return await _context.Organizations
            .Include(o => o.Owner)
            .Include(o => o.Members)
                .ThenInclude(m => m.AppUser)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Organization?> GetOrganizationByOwnerId(int userId)
    {
        // First check if user is an owner
        var org = await _context.Organizations
            .Include(o => o.Owner)
            .Include(o => o.Members)
                .ThenInclude(m => m.AppUser)
            .FirstOrDefaultAsync(o => o.OwnerId == userId && o.IsActive);

        if (org != null) return org;

        // If not an owner, find organization through membership
        var membership = await _context.OrganizationMembers
            .Include(m => m.Organization)
                .ThenInclude(o => o.Owner)
            .Include(m => m.Organization)
                .ThenInclude(o => o.Members)
                    .ThenInclude(m => m.AppUser)
            .FirstOrDefaultAsync(m => m.AppUserId == userId && m.Organization.IsActive);

        return membership?.Organization;
    }

    public async Task<List<Organization>> GetAllOrganizations()
    {
        return await _context.Organizations
            .Include(o => o.Owner)
            .Include(o => o.Members)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Organization> UpdateOrganization(int id, string name)
    {
        var organization = await _context.Organizations.FindAsync(id);
        if (organization == null)
        {
            throw new ArgumentException($"Organization with id {id} not found.");
        }

        organization.Name = name;
        organization.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization {OrgId} updated: Name='{Name}'", id, name);

        return organization;
    }

    public async Task<Organization> UpdateOrganization(int id, Organization updatedFields)
    {
        var organization = await _context.Organizations.FindAsync(id);
        if (organization == null)
        {
            throw new ArgumentException($"Organization with id {id} not found.");
        }

        ApplyBusinessFields(organization, updatedFields);
        organization.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization {OrgId} business info updated", id);

        return organization;
    }

    public async Task<bool> DeactivateOrganization(int id)
    {
        var organization = await _context.Organizations.FindAsync(id);
        if (organization == null) return false;

        organization.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization {OrgId} deactivated", id);

        return true;
    }

    /// <summary>
    /// Copies non-empty string fields from source into target.
    /// Only overwrites fields that have a non-empty value in the source.
    /// </summary>
    private static void ApplyBusinessFields(Organization target, Organization source)
    {
        if (!string.IsNullOrWhiteSpace(source.Name))
            target.Name = source.Name;

        // Business Registration
        if (!string.IsNullOrWhiteSpace(source.CommercialRegistrationNumber))
            target.CommercialRegistrationNumber = source.CommercialRegistrationNumber;
        if (!string.IsNullOrWhiteSpace(source.VatNumber))
            target.VatNumber = source.VatNumber;
        if (!string.IsNullOrWhiteSpace(source.LegalName))
            target.LegalName = source.LegalName;

        // Legal Address
        if (!string.IsNullOrWhiteSpace(source.BuildingNumber))
            target.BuildingNumber = source.BuildingNumber;
        if (!string.IsNullOrWhiteSpace(source.StreetName))
            target.StreetName = source.StreetName;
        if (!string.IsNullOrWhiteSpace(source.District))
            target.District = source.District;
        if (!string.IsNullOrWhiteSpace(source.City))
            target.City = source.City;
        if (!string.IsNullOrWhiteSpace(source.PostalCode))
            target.PostalCode = source.PostalCode;
        if (!string.IsNullOrWhiteSpace(source.CountryCode))
            target.CountryCode = source.CountryCode;

        // Contact Info
        if (!string.IsNullOrWhiteSpace(source.Email))
            target.Email = source.Email;
        if (!string.IsNullOrWhiteSpace(source.Phone))
            target.Phone = source.Phone;
        if (!string.IsNullOrWhiteSpace(source.WhatsApp))
            target.WhatsApp = source.WhatsApp;
        if (!string.IsNullOrWhiteSpace(source.Website))
            target.Website = source.Website;

        // Bank Account
        if (!string.IsNullOrWhiteSpace(source.BankName))
            target.BankName = source.BankName;
        if (!string.IsNullOrWhiteSpace(source.BankAccountNumber))
            target.BankAccountNumber = source.BankAccountNumber;
        if (!string.IsNullOrWhiteSpace(source.BankIban))
            target.BankIban = source.BankIban;

        // Always apply commission rate (null = use platform default)
        target.CommissionRate = source.CommissionRate;
    }
}
