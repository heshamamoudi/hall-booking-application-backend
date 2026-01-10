using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IHallManagerRepository : IGenericRepository<HallManager>
{
    Task<HallManager> GetByUserIdAsync(string userId);
}
