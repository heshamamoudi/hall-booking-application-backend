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
        
        await _unitOfWork.HallManagerRepository.AddAsync(hallManager);
        await _unitOfWork.Complete();
        return hallManager;
    }

    public async Task<HallManager> UpdateHallManagerAsync(HallManager hallManager)
    {
        var existingHallManager = await _unitOfWork.HallManagerRepository.GetByIdAsync(hallManager.Id);
        // No duplicate check needed - HallManager is just a link entity

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

    public async Task<HallManager> GetHallManagerByIdAsync(int hallManagerId)
    {
        return await _unitOfWork.HallManagerRepository.GetByIdAsync(hallManagerId);
    }

    public async Task<HallManager> GetHallManagerByAppUserIdAsync(int appUserId)
    {
        var allHallManagers = await _unitOfWork.HallManagerRepository.GetAllAsync();
        return allHallManagers.FirstOrDefault(hm => hm.AppUserId == appUserId) ?? new HallManager();
    }

    public async Task<List<HallManager>> GetAllHallManagersAsync()
    {
        var hallManagers = await _unitOfWork.HallManagerRepository.GetAllAsync();
        return hallManagers.ToList();
    }

    // Business relationships
    public async Task<int> GetHallManagerHallCountAsync(int hallManagerId)
    {
        var hallManager = await _unitOfWork.HallManagerRepository.GetByIdAsync(hallManagerId);
        if (hallManager == null) return 0;
        return hallManager.Halls?.Count ?? 0;
    }
}
