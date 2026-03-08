using HallApp.Core.Entities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IPlatformSettingsRepository
{
    Task<PlatformSettings?> GetActiveAsync();
    Task<PlatformSettings> AddAsync(PlatformSettings entity);
    void Update(PlatformSettings entity);
}
