using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Entities;

namespace HallApp.Core.Interfaces.IServices
{
    /// <summary>
    /// Combined service for operations requiring both AppUser + VendorManager data
    /// Used for vendor profile management, vendor dashboard, etc.
    /// Core layer interface - uses entities, not DTOs
    /// </summary>
    public interface IVendorProfileService
    {
        // Profile operations (combines AppUser + VendorManager)
        Task<(AppUser AppUser, VendorManager VendorManager)> GetVendorProfileAsync(string userId);
        Task<bool> UpdateVendorProfileAsync(string userId, AppUser userData, VendorManager vendorManagerData);
        Task<bool> DeleteVendorProfileAsync(string userId);
        
        // Authentication-related profile operations
        Task<bool> UpdatePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<bool> VerifyEmailAsync(string userId, string token);
        Task<bool> SendVerificationEmailAsync(string userId);
        
        // Business profile operations
        Task<(AppUser AppUser, VendorManager VendorManager)> GetVendorDashboardAsync(string userId);
        Task<bool> UpdateBusinessPreferencesAsync(string userId, VendorManager businessData);
        Task<bool> ApproveVendorManagerAsync(string userId, bool isApproved);
    }
}
