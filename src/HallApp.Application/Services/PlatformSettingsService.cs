using HallApp.Core.Entities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

public class PlatformSettingsService : IPlatformSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PlatformSettingsService> _logger;
    private const string CacheKey = "PlatformSettings_Active";

    public PlatformSettingsService(IUnitOfWork unitOfWork, IMemoryCache cache, ILogger<PlatformSettingsService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PlatformSettings> GetSettingsAsync()
    {
        if (_cache.TryGetValue(CacheKey, out PlatformSettings? cached) && cached != null)
            return cached;

        var settings = await _unitOfWork.PlatformSettingsRepository.GetActiveAsync();

        if (settings == null)
        {
            _logger.LogInformation("No platform settings found, creating defaults");
            settings = new PlatformSettings
            {
                CompanyName = "Zawaji",
                LegalName = "Zawaji",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System"
            };
            await _unitOfWork.PlatformSettingsRepository.AddAsync(settings);
            await _unitOfWork.Complete();
        }

        _cache.Set(CacheKey, settings, TimeSpan.FromMinutes(5));
        return settings;
    }

    public async Task<PlatformSettings> UpdateSettingsAsync(PlatformSettings settings, string updatedBy)
    {
        var existing = await _unitOfWork.PlatformSettingsRepository.GetActiveAsync();
        if (existing == null)
            throw new InvalidOperationException("Platform settings not found");

        existing.CompanyName = settings.CompanyName;
        existing.LegalName = settings.LegalName;
        existing.VatNumber = settings.VatNumber;
        existing.CommercialRegistrationNumber = settings.CommercialRegistrationNumber;
        existing.BuildingNumber = settings.BuildingNumber;
        existing.StreetName = settings.StreetName;
        existing.District = settings.District;
        existing.City = settings.City;
        existing.PostalCode = settings.PostalCode;
        existing.CountryCode = settings.CountryCode;
        existing.Email = settings.Email;
        existing.Phone = settings.Phone;
        existing.WhatsApp = settings.WhatsApp;
        existing.Website = settings.Website;
        existing.BankName = settings.BankName;
        existing.BankAccountNumber = settings.BankAccountNumber;
        existing.BankIban = settings.BankIban;
        existing.LogoUrl = settings.LogoUrl;
        existing.DefaultHallCommissionRate = settings.DefaultHallCommissionRate;
        existing.DefaultVendorCommissionRate = settings.DefaultVendorCommissionRate;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = updatedBy;

        _unitOfWork.PlatformSettingsRepository.Update(existing);
        await _unitOfWork.Complete();

        _cache.Remove(CacheKey);
        _logger.LogInformation("Platform settings updated by {UpdatedBy}", updatedBy);

        return existing;
    }

    public decimal GetEffectiveCommissionRate(Organization? organization, string supplierType, PlatformSettings platformSettings)
    {
        // Organization-specific override takes priority
        if (organization?.CommissionRate != null)
            return organization.CommissionRate.Value;

        // Fall back to platform defaults
        return supplierType.Equals("Hall", StringComparison.OrdinalIgnoreCase)
            ? platformSettings.DefaultHallCommissionRate
            : platformSettings.DefaultVendorCommissionRate;
    }
}
