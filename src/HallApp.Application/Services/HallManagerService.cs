using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Application.Services;

/// <summary>
/// Service for HallManager business domain operations only
/// Focuses purely on HallManager entity business logic, separate from AppUser concerns
/// </summary>
public class HallManagerService : IHallManagerService
{
    private readonly IUnitOfWork _unitOfWork;

    public HallManagerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Core CRUD operations
    public async Task<HallManager> CreateHallManagerAsync(HallManager hallManager)
    {
        hallManager.CreatedAt = DateTime.UtcNow;
        hallManager.IsApproved = false; // Default to pending approval
        
        await _unitOfWork.HallManagerRepository.AddAsync(hallManager);
        await _unitOfWork.Complete();
        return hallManager;
    }

    public async Task<HallManager?> UpdateHallManagerAsync(HallManager hallManager)
    {
        var existingHallManager = await _unitOfWork.HallManagerRepository.GetByIdAsync(hallManager.Id);
        if (existingHallManager == null) return null;

        // Update business domain properties
        existingHallManager.CompanyName = hallManager.CompanyName;
        existingHallManager.CommercialRegistrationNumber = hallManager.CommercialRegistrationNumber;
        existingHallManager.IsApproved = hallManager.IsApproved;
        if (hallManager.IsApproved && existingHallManager.ApprovedAt == null)
        {
            existingHallManager.ApprovedAt = DateTime.UtcNow;
        }

        _unitOfWork.HallManagerRepository.Update(existingHallManager);
        await _unitOfWork.Complete();
        return existingHallManager;
    }

    public async Task<bool> DeleteHallManagerAsync(int hallManagerId)
    {
        var hallManager = await _unitOfWork.HallManagerRepository.GetByIdAsync(hallManagerId);
        if (hallManager == null) return false;

        _unitOfWork.HallManagerRepository.Delete(hallManager);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<HallManager?> GetHallManagerByIdAsync(int hallManagerId)
    {
        return await _unitOfWork.HallManagerRepository.GetByIdAsync(hallManagerId);
    }

    public async Task<HallManager?> GetHallManagerByAppUserIdAsync(int appUserId)
    {
        var allHallManagers = await _unitOfWork.HallManagerRepository.GetAllAsync();
        return allHallManagers.FirstOrDefault(hm => hm.AppUserId == appUserId);
    }

    public async Task<List<HallManager>> GetAllHallManagersAsync()
    {
        var hallManagers = await _unitOfWork.HallManagerRepository.GetAllAsync();
        return hallManagers.ToList();
    }

    // Business domain operations
    public async Task<bool> ApproveHallManagerAsync(int hallManagerId, bool isApproved)
    {
        var hallManager = await _unitOfWork.HallManagerRepository.GetByIdAsync(hallManagerId);
        if (hallManager == null) return false;

        hallManager.IsApproved = isApproved;
        hallManager.ApprovedAt = isApproved ? DateTime.UtcNow : null;

        _unitOfWork.HallManagerRepository.Update(hallManager);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<List<HallManager>> GetPendingApprovalHallManagersAsync()
    {
        var allHallManagers = await _unitOfWork.HallManagerRepository.GetAllAsync();
        return allHallManagers.Where(hm => !hm.IsApproved).ToList();
    }

    public async Task<List<HallManager>> GetApprovedHallManagersAsync()
    {
        var allHallManagers = await _unitOfWork.HallManagerRepository.GetAllAsync();
        return allHallManagers.Where(hm => hm.IsApproved).ToList();
    }

    public async Task<bool> UpdateCompanyInfoAsync(int hallManagerId, string companyName, string registrationNumber)
    {
        var hallManager = await _unitOfWork.HallManagerRepository.GetByIdAsync(hallManagerId);
        if (hallManager == null) return false;

        hallManager.CompanyName = companyName;
        hallManager.CommercialRegistrationNumber = registrationNumber;

        _unitOfWork.HallManagerRepository.Update(hallManager);
        await _unitOfWork.Complete();
        return true;
    }

    // Validation methods
    public async Task<bool> IsCommercialRegistrationUniqueAsync(string registrationNumber, int? excludeId = null)
    {
        if (string.IsNullOrEmpty(registrationNumber))
            return false;

        var allHallManagers = await _unitOfWork.HallManagerRepository.GetAllAsync();
        return !allHallManagers.Any(hm => 
            hm.CommercialRegistrationNumber == registrationNumber && 
            (excludeId == null || hm.Id != excludeId));
    }

    public async Task<bool> IsCompanyNameUniqueAsync(string companyName, int? excludeId = null)
    {
        if (string.IsNullOrEmpty(companyName))
            return false;

        var allHallManagers = await _unitOfWork.HallManagerRepository.GetAllAsync();
        return !allHallManagers.Any(hm => 
            hm.CompanyName == companyName && 
            (excludeId == null || hm.Id != excludeId));
    }

    // Business relationships
    public async Task<int> GetHallManagerHallCountAsync(int hallManagerId)
    {
        // This would typically query the Hall repository to count halls for this manager
        // For now, return 0 as placeholder
        return await Task.FromResult(0);
    }

    public async Task<List<HallManager>> GetHallManagersByStatusAsync(bool isApproved)
    {
        var allHallManagers = await _unitOfWork.HallManagerRepository.GetAllAsync();
        return allHallManagers.Where(hm => hm.IsApproved == isApproved).ToList();
    }
}
