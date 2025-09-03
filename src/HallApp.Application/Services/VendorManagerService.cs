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
        vendorManager.IsApproved = false; // Default to pending approval
        
        _unitOfWork.VendorManagerRepository.Add(vendorManager);
        await _unitOfWork.Complete();
        return vendorManager;
    }

    public async Task<VendorManager> UpdateVendorManagerAsync(VendorManager vendorManager)
    {
        var existingVendorManager = await _unitOfWork.VendorManagerRepository.GetByIdAsync(vendorManager.Id);
        if (existingVendorManager == null) return new VendorManager();

        // Update business domain properties
        existingVendorManager.CommercialRegistrationNumber = vendorManager.CommercialRegistrationNumber;
        existingVendorManager.VatNumber = vendorManager.VatNumber;
        existingVendorManager.IsApproved = vendorManager.IsApproved;
        if (vendorManager.IsApproved && existingVendorManager.ApprovedAt == null)
        {
            existingVendorManager.ApprovedAt = DateTime.UtcNow;
        }

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

    // Business domain operations
    public async Task<bool> ApproveVendorManagerAsync(int vendorManagerId, bool isApproved)
    {
        var vendorManager = await _unitOfWork.VendorManagerRepository.GetByIdAsync(vendorManagerId);
        if (vendorManager == null) return false;

        vendorManager.IsApproved = isApproved;
        vendorManager.ApprovedAt = isApproved ? DateTime.UtcNow : null;

        _unitOfWork.VendorManagerRepository.Update(vendorManager);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<List<VendorManager>> GetPendingApprovalVendorManagersAsync()
    {
        var allVendorManagers = await _unitOfWork.VendorManagerRepository.GetAllAsync();
        return allVendorManagers.Where(vm => !vm.IsApproved).ToList();
    }

    public async Task<List<VendorManager>> GetApprovedVendorManagersAsync()
    {
        var allVendorManagers = await _unitOfWork.VendorManagerRepository.GetAllAsync();
        return allVendorManagers.Where(vm => vm.IsApproved).ToList();
    }

    public async Task<bool> UpdateCommercialInfoAsync(int vendorManagerId, string registrationNumber = "", string vatNumber = "")
    {
        var vendorManager = await _unitOfWork.VendorManagerRepository.GetByIdAsync(vendorManagerId);
        if (vendorManager == null) return false;

        vendorManager.CommercialRegistrationNumber = registrationNumber;
        vendorManager.VatNumber = vatNumber;

        _unitOfWork.VendorManagerRepository.Update(vendorManager);
        await _unitOfWork.Complete();
        return true;
    }

    // Validation methods
    public async Task<bool> IsCommercialRegistrationUniqueAsync(string registrationNumber, int excludeId = 0)
    {
        if (string.IsNullOrEmpty(registrationNumber))
            return false;
            
        var allVendorManagers = await _unitOfWork.VendorManagerRepository.GetAllAsync();
        return !allVendorManagers.Any(vm => 
            vm.CommercialRegistrationNumber == registrationNumber && 
            (excludeId == 0 || vm.Id != excludeId));
    }

    public async Task<bool> IsVatNumberUniqueAsync(string vatNumber, int excludeId = 0)
    {
        if (string.IsNullOrEmpty(vatNumber))
            return false;
            
        var allVendorManagers = await _unitOfWork.VendorManagerRepository.GetAllAsync();
        return !allVendorManagers.Any(vm => 
            vm.VatNumber == vatNumber && 
            (excludeId == 0 || vm.Id != excludeId));
    }

    // Business relationships
    public async Task<int> GetVendorManagerVendorCountAsync(int vendorManagerId)
    {
        // This would typically query the Vendor repository to count vendors for this manager
        // For now, return 0 as placeholder
        return await Task.FromResult(0);
    }

    public async Task<List<VendorManager>> GetVendorManagersByStatusAsync(bool isApproved)
    {
        var allVendorManagers = await _unitOfWork.VendorManagerRepository.GetAllAsync();
        return allVendorManagers.Where(vm => vm.IsApproved == isApproved).ToList();
    }
}
