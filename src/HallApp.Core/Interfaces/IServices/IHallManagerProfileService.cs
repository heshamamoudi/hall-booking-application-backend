using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities;

namespace HallApp.Core.Interfaces.IServices
{
    /// <summary>
    /// Combined service for operations requiring both AppUser + HallManager data
    /// Used for hall manager profile management, hall manager dashboard, etc.
    /// Core layer interface - uses entities, not DTOs
    /// </summary>
    public interface IHallManagerProfileService
    {
        // Profile operations (combines AppUser + HallManager)
        Task<(AppUser AppUser, HallManager HallManager)?> GetHallManagerProfileAsync(string userId);
        Task<bool> UpdateHallManagerProfileAsync(string userId, AppUser userData, HallManager hallManagerData);
        Task<bool> DeleteHallManagerProfileAsync(string userId);
        
        // Authentication-related profile operations
        Task<bool> UpdatePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<bool> VerifyEmailAsync(string userId, string token);
        Task<bool> SendVerificationEmailAsync(string userId);
        
        // Business profile operations
        Task<(AppUser AppUser, HallManager HallManager)?> GetHallManagerDashboardAsync(string userId);
        Task<bool> UpdateBusinessPreferencesAsync(string userId, HallManager businessData);
    }
}
