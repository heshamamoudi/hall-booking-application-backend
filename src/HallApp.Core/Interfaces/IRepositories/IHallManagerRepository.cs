using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IHallManagerRepository : IGenericRepository<HallManager>
{
    /// <summary>
    /// Gets a HallManager by the associated AppUser ID with Halls included.
    /// Performs a direct database query instead of loading all managers.
    /// </summary>
    Task<HallManager?> GetByAppUserIdWithHallsAsync(int appUserId);

    /// <summary>
    /// Gets a HallManager by the associated user ID string (legacy support).
    /// </summary>
    Task<HallManager?> GetByUserIdAsync(string userId);

    /// <summary>
    /// Gets HallManagers for a batch of AppUserIds using AsNoTracking.
    /// HIGH-6 FIX: Enables building AppUserId-to-HallManagerId mapping in a single query
    /// instead of loading all managers per-member.
    /// </summary>
    Task<List<HallManager>> GetByAppUserIdsAsync(IEnumerable<int> appUserIds);
}
