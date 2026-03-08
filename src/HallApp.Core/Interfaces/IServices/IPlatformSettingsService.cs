using HallApp.Core.Entities;

namespace HallApp.Core.Interfaces.IServices;

public interface IPlatformSettingsService
{
    Task<PlatformSettings> GetSettingsAsync();
    Task<PlatformSettings> UpdateSettingsAsync(PlatformSettings settings, string updatedBy);
    decimal GetEffectiveCommissionRate(Organization? organization, string supplierType, PlatformSettings platformSettings);
}
