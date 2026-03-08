using HallApp.Core.Entities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class PlatformSettingsRepository : IPlatformSettingsRepository
{
    private readonly DataContext _context;

    public PlatformSettingsRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<PlatformSettings?> GetActiveAsync()
    {
        return await _context.PlatformSettings
            .FirstOrDefaultAsync(ps => ps.IsActive);
    }

    public async Task<PlatformSettings> AddAsync(PlatformSettings entity)
    {
        var result = await _context.PlatformSettings.AddAsync(entity);
        return result.Entity;
    }

    public void Update(PlatformSettings entity)
    {
        _context.PlatformSettings.Update(entity);
    }
}
