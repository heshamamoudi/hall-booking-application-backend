using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Entities;

namespace HallApp.Core.Interfaces.IServices
{
    /// <summary>
    /// Combined service for operations requiring both AppUser + Customer data
    /// Used for profile management, customer dashboard, etc.
    /// Core layer interface - uses entities, not DTOs
    /// </summary>
    public interface ICustomerProfileService
    {
        // Profile operations (combines AppUser + Customer)
        Task<(AppUser AppUser, Customer Customer)?> GetCustomerProfileAsync(string userId);
        Task<bool> UpdateCustomerProfileAsync(string userId, AppUser userData, Customer customerData);
        Task<bool> DeleteCustomerProfileAsync(string userId);
        
        // Authentication-related profile operations
        Task<bool> UpdatePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<bool> VerifyEmailAsync(string userId, string token);
        Task<bool> SendVerificationEmailAsync(string userId);
        
        // Business profile operations
        Task<(AppUser AppUser, Customer Customer)?> GetCustomerDashboardAsync(string userId);
        Task<bool> UpdateBusinessPreferencesAsync(string userId, Customer businessData);
    }
}
