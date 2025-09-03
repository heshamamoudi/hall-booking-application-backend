using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IHallManagerRepository : IGenericRepository<HallManager>
{
    Task<HallManager> GetByUserIdAsync(string userId);
    Task<bool> CommercialRegistrationExistsAsync(string registrationNumber);
    Task<bool> CompanyNameExistsAsync(string companyName);
    Task<IEnumerable<HallManager>> GetPendingApprovalAsync();
    Task<IEnumerable<HallManager>> GetApprovedAsync();
}
