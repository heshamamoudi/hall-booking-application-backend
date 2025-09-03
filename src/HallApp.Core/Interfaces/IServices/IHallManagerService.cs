using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Interfaces.IServices
{
    /// <summary>
    /// Service interface for HallManager business domain operations only
    /// Focuses purely on HallManager entity business logic
    /// </summary>
    public interface IHallManagerService
    {
        // Core CRUD operations
        Task<HallManager> CreateHallManagerAsync(HallManager hallManager);
        Task<HallManager> UpdateHallManagerAsync(HallManager hallManager);
        Task<bool> DeleteHallManagerAsync(int hallManagerId);
        Task<HallManager> GetHallManagerByIdAsync(int hallManagerId);
        Task<HallManager> GetHallManagerByAppUserIdAsync(int appUserId);
        Task<List<HallManager>> GetAllHallManagersAsync();
        
        // Business domain operations
        Task<bool> ApproveHallManagerAsync(int hallManagerId, bool isApproved);
        Task<List<HallManager>> GetPendingApprovalHallManagersAsync();
        Task<List<HallManager>> GetApprovedHallManagersAsync();
        Task<bool> UpdateCompanyInfoAsync(int hallManagerId, string companyName, string registrationNumber);
        
        // Validation methods
        Task<bool> IsCommercialRegistrationUniqueAsync(string registrationNumber, int excludeId = 0);
        Task<bool> IsCompanyNameUniqueAsync(string companyName, int excludeId = 0);
        
        // Business relationships
        Task<int> GetHallManagerHallCountAsync(int hallManagerId);
        Task<List<HallManager>> GetHallManagersByStatusAsync(bool isApproved);
    }
}
