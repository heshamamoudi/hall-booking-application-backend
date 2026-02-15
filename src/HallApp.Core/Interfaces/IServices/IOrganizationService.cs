using HallApp.Core.Entities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// Service interface for organization management operations.
/// Handles CRUD operations on organizations owned by HallOrganizationManagers or VendorOrganizationManagers.
/// </summary>
public interface IOrganizationService
{
    Task<Organization> CreateOrganization(string name, string type, int ownerId);

    /// <summary>
    /// Creates an organization with business registration info pre-populated.
    /// The businessInfo parameter is an <see cref="Organization"/> whose non-empty string fields
    /// are copied into the new entity.
    /// </summary>
    Task<Organization> CreateOrganization(string name, string type, int ownerId, Organization businessInfo);

    Task<Organization> GetOrganizationById(int id);
    Task<Organization> GetOrganizationByOwnerId(int userId);
    Task<List<Organization>> GetAllOrganizations();
    Task<Organization> UpdateOrganization(int id, string name);

    /// <summary>
    /// Updates organization details including business registration, address, contact, and bank info.
    /// The updatedFields parameter is an <see cref="Organization"/> whose non-empty string fields
    /// will overwrite the existing values.
    /// </summary>
    Task<Organization> UpdateOrganization(int id, Organization updatedFields);

    Task<bool> DeactivateOrganization(int id);
}
