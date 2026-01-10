using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Application.Services;

/// <summary>
/// Service for VendorManager business domain operations only
/// Focuses purely on VendorManager entity business logic, separate from AppUser concerns
/// </summary>
public class VendorManagerService : IVendorManagerService
{
    private readonly IUnitOfWork _unitOfWork;

    public VendorManagerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Core CRUD operations
    public async Task<VendorManager> CreateVendorManagerAsync(VendorManager vendorManager)
    {
        vendorManager.CreatedAt = DateTime.UtcNow;
        
        await _unitOfWork.VendorManagerRepository.AddAsync(vendorManager);
        await _unitOfWork.Complete();
        return vendorManager;
    }

    public async Task<VendorManager> UpdateVendorManagerAsync(VendorManager vendorManager)
    {
        var existingVendorManager = await _unitOfWork.VendorManagerRepository.GetByIdAsync(vendorManager.Id);
        if (existingVendorManager == null) return new VendorManager();

        // VendorManager is just a link entity - no business properties to update
        _unitOfWork.VendorManagerRepository.Update(existingVendorManager);
        await _unitOfWork.Complete();
        return existingVendorManager;
    }

    public async Task<bool> DeleteVendorManagerAsync(int vendorManagerId)
    {
        var vendorManager = await _unitOfWork.VendorManagerRepository.GetByIdAsync(vendorManagerId);
        if (vendorManager == null) return false;

        _unitOfWork.VendorManagerRepository.Delete(vendorManager);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<VendorManager> GetVendorManagerByIdAsync(int vendorManagerId)
    {
        return await _unitOfWork.VendorManagerRepository.GetByIdAsync(vendorManagerId);
    }

    public async Task<VendorManager> GetVendorManagerByAppUserIdAsync(int appUserId)
    {
        var allVendorManagers = await _unitOfWork.VendorManagerRepository.GetAllAsync();
        return allVendorManagers.FirstOrDefault(vm => vm.AppUserId == appUserId);
    }

    public async Task<List<VendorManager>> GetAllVendorManagersAsync()
    {
        var vendorManagers = await _unitOfWork.VendorManagerRepository.GetAllAsync();
        return vendorManagers.ToList();
    }

    // Business relationships
    public async Task<int> GetVendorManagerVendorCountAsync(int vendorManagerId)
    {
        var vendorManager = await _unitOfWork.VendorManagerRepository.GetByIdAsync(vendorManagerId);
        if (vendorManager == null) return 0;
        return vendorManager.Vendors?.Count ?? 0;
    }
}
