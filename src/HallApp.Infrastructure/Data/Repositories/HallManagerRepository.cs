using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class HallManagerRepository : GenericRepository<HallManager>, IHallManagerRepository
{
    public HallManagerRepository(DataContext context) : base(context)
    {
    }

    public async Task<HallManager?> GetByUserIdAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return null;
            
        return await _context.HallManagers.FirstOrDefaultAsync(hm => hm.AppUserId == userIdInt);
    }

    public async Task<bool> CommercialRegistrationExistsAsync(string registrationNumber)
    {
        if (string.IsNullOrEmpty(registrationNumber))
            return false;
            
        return await _context.HallManagers.AnyAsync(hm => hm.CommercialRegistrationNumber == registrationNumber);
    }

    public async Task<bool> CompanyNameExistsAsync(string companyName)
    {
        if (string.IsNullOrEmpty(companyName))
            return false;
            
        return await _context.HallManagers.AnyAsync(hm => hm.CompanyName == companyName);
    }

    public async Task<IEnumerable<HallManager>> GetPendingApprovalAsync()
    {
        return await _context.HallManagers.Where(hm => !hm.IsApproved).ToListAsync();
    }

    public async Task<IEnumerable<HallManager>> GetApprovedAsync()
    {
        return await _context.HallManagers.Where(hm => hm.IsApproved).ToListAsync();
    }
}
