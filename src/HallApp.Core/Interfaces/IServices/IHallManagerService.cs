using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Interfaces.IServices
{
    /// <summary>
    /// Service interface for HallManager operations
    /// HallManager is just a link between AppUser and managed Halls
    /// Business properties (approval, commercial reg) are on Hall entity
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
        
        // Business relationships
        Task<int> GetHallManagerHallCountAsync(int hallManagerId);
    }
}
