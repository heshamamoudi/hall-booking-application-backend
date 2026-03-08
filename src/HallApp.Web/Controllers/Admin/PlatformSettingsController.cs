using HallApp.Application.DTOs.Admin;
using HallApp.Core.Entities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Admin;

/// <summary>
/// Manages platform-wide settings including company identity, commission rates, and contact information.
/// Admin-only access.
/// </summary>
[Authorize(Roles = "Admin")]
[Route("api/v1/platform-settings")]
public class PlatformSettingsController : BaseApiController
{
    private readonly IPlatformSettingsService _platformSettingsService;

    public PlatformSettingsController(IPlatformSettingsService platformSettingsService)
    {
        _platformSettingsService = platformSettingsService;
    }

    /// <summary>
    /// Gets the current active platform settings.
    /// Creates default settings if none exist.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PlatformSettingsDto>>> GetSettings()
    {
        var settings = await _platformSettingsService.GetSettingsAsync();
        var dto = MapToDto(settings);
        return Success(dto);
    }

    /// <summary>
    /// Updates the platform settings.
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<PlatformSettingsDto>>> UpdateSettings(
        [FromBody] UpdatePlatformSettingsDto updateDto)
    {
        var settingsEntity = new PlatformSettings
        {
            CompanyName = updateDto.CompanyName,
            LegalName = updateDto.LegalName,
            VatNumber = updateDto.VatNumber,
            CommercialRegistrationNumber = updateDto.CommercialRegistrationNumber,
            BuildingNumber = updateDto.BuildingNumber,
            StreetName = updateDto.StreetName,
            District = updateDto.District,
            City = updateDto.City,
            PostalCode = updateDto.PostalCode,
            CountryCode = updateDto.CountryCode,
            Email = updateDto.Email,
            Phone = updateDto.Phone,
            WhatsApp = updateDto.WhatsApp,
            Website = updateDto.Website,
            BankName = updateDto.BankName,
            BankAccountNumber = updateDto.BankAccountNumber,
            BankIban = updateDto.BankIban,
            LogoUrl = updateDto.LogoUrl,
            DefaultHallCommissionRate = updateDto.DefaultHallCommissionRate,
            DefaultVendorCommissionRate = updateDto.DefaultVendorCommissionRate
        };

        var updated = await _platformSettingsService.UpdateSettingsAsync(settingsEntity, UserName);
        var dto = MapToDto(updated);
        return Success(dto, "Platform settings updated successfully");
    }

    private static PlatformSettingsDto MapToDto(PlatformSettings entity)
    {
        return new PlatformSettingsDto
        {
            Id = entity.Id,
            CompanyName = entity.CompanyName,
            LegalName = entity.LegalName,
            VatNumber = entity.VatNumber,
            CommercialRegistrationNumber = entity.CommercialRegistrationNumber,
            BuildingNumber = entity.BuildingNumber,
            StreetName = entity.StreetName,
            District = entity.District,
            City = entity.City,
            PostalCode = entity.PostalCode,
            CountryCode = entity.CountryCode,
            Email = entity.Email,
            Phone = entity.Phone,
            WhatsApp = entity.WhatsApp,
            Website = entity.Website,
            BankName = entity.BankName,
            BankAccountNumber = entity.BankAccountNumber,
            BankIban = entity.BankIban,
            LogoUrl = entity.LogoUrl,
            DefaultHallCommissionRate = entity.DefaultHallCommissionRate,
            DefaultVendorCommissionRate = entity.DefaultVendorCommissionRate,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
    }
}
