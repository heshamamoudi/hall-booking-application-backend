using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;

namespace HallApp.Application.Services;

public class VendorService : IVendorService
{
    private readonly IUnitOfWork _unitOfWork;

    public VendorService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Vendor> CreateVendorAsync(Vendor vendor)
    {
        var createdVendor = await _unitOfWork.VendorRepository.CreateVendor(vendor);
        await _unitOfWork.Complete();
        return createdVendor;
    }

    public async Task<Vendor> UpdateVendorAsync(int id, Vendor vendor)
    {
        var existingVendor = await _unitOfWork.VendorRepository.GetVendorByIdAsync(id);
        if (existingVendor == null) throw new ArgumentException($"Vendor with id {id} not found");
        
        // Update scalar properties
        existingVendor.Name = vendor.Name;
        existingVendor.Description = vendor.Description;
        existingVendor.Email = vendor.Email;
        existingVendor.Phone = vendor.Phone;
        existingVendor.WhatsApp = vendor.WhatsApp ?? existingVendor.WhatsApp;
        existingVendor.Website = vendor.Website ?? existingVendor.Website;
        existingVendor.LogoUrl = vendor.LogoUrl ?? existingVendor.LogoUrl;
        existingVendor.CoverImageUrl = vendor.CoverImageUrl ?? existingVendor.CoverImageUrl;
        existingVendor.VendorTypeId = vendor.VendorTypeId;
        existingVendor.IsActive = vendor.IsActive;
        
        // Update flags
        existingVendor.HasSpecialOffer = vendor.HasSpecialOffer;
        existingVendor.IsFeatured = vendor.IsFeatured;
        existingVendor.IsPremium = vendor.IsPremium;
        
        // Update timestamp
        existingVendor.UpdatedAt = DateTime.UtcNow;
        
        // Update Location if provided
        if (vendor.Location != null)
        {
            if (existingVendor.Location == null)
            {
                existingVendor.Location = vendor.Location;
            }
            else
            {
                existingVendor.Location.Latitude = vendor.Location.Latitude;
                existingVendor.Location.Longitude = vendor.Location.Longitude;
                existingVendor.Location.City = vendor.Location.City;
                existingVendor.Location.Address = vendor.Location.Address;
                existingVendor.Location.State = vendor.Location.State;
                existingVendor.Location.Country = vendor.Location.Country;
                existingVendor.Location.PostalCode = vendor.Location.PostalCode;
            }
        }
        
        // Update Managers collection if provided
        if (vendor.Managers != null)
        {
            existingVendor.Managers?.Clear();
            existingVendor.Managers = vendor.Managers;
        }
        
        // Update ServiceItems collection if provided
        if (vendor.ServiceItems != null)
        {
            existingVendor.ServiceItems?.Clear();
            existingVendor.ServiceItems = vendor.ServiceItems;
        }
        
        // Update BusinessHours collection if provided
        if (vendor.BusinessHours != null)
        {
            existingVendor.BusinessHours?.Clear();
            existingVendor.BusinessHours = vendor.BusinessHours;
        }
        
        // Update BlockedDates collection if provided
        if (vendor.BlockedDates != null)
        {
            existingVendor.BlockedDates?.Clear();
            existingVendor.BlockedDates = vendor.BlockedDates;
        }
        
        await _unitOfWork.Complete();
        return existingVendor;
    }

    public async Task<Vendor> ToggleVendorActiveAsync(int id, bool isActive)
    {
        var existingVendor = await _unitOfWork.VendorRepository.GetVendorByIdAsync(id);
        if (existingVendor == null) return null;
        
        // Only update IsActive field to prevent data loss
        existingVendor.IsActive = isActive;
        
        await _unitOfWork.Complete();
        return existingVendor;
    }

    public async Task<bool> DeleteVendorAsync(int id)
    {
        var result = await _unitOfWork.VendorRepository.DeleteVendorAsync(id);
        await _unitOfWork.Complete();
        return result;
    }

    public async Task<Vendor> GetVendorByIdAsync(int id)
    {
        return await _unitOfWork.VendorRepository.GetVendorByIdAsync(id);
    }

    public async Task<IEnumerable<Vendor>> GetVendorsAsync(object vendorParams)
    {
        return await _unitOfWork.VendorRepository.GetAllVendorsAsync();
    }

    public async Task<List<Vendor>> GetVendorsByTypeAsync(int typeId)
    {
        return await _unitOfWork.VendorRepository.GetVendorsByTypeAsync(typeId);
    }

    public async Task<List<Vendor>> GetVendorsByManagerAsync(int managerId)
    {
        return await _unitOfWork.VendorRepository.GetVendorsByManagerAsync(managerId);
    }

    public async Task<List<Vendor>> SearchVendorsAsync(string searchTerm)
    {
        return await _unitOfWork.VendorRepository.SearchVendorsAsync(searchTerm);
    }
    
    public async Task<bool> IsNameUniqueAsync(string name, int excludeId = 0)
    {
        if (string.IsNullOrEmpty(name))
            return false;
            
        return await _unitOfWork.VendorRepository.IsNameUniqueAsync(name, excludeId);
    }
    
    public async Task<bool> IsEmailUniqueAsync(string email, int excludeId = 0)
    {
        if (string.IsNullOrEmpty(email))
            return false;
            
        return await _unitOfWork.VendorRepository.IsEmailUniqueAsync(email, excludeId);
    }
    
    public async Task<bool> IsPhoneUniqueAsync(string phone, int excludeId = 0)
    {
        return await _unitOfWork.VendorRepository.IsPhoneUniqueAsync(phone, excludeId);
    }

    public async Task<List<VendorType>> GetVendorTypesAsync()
    {
        return await _unitOfWork.VendorRepository.GetVendorTypesAsync();
    }

    public async Task<VendorType> GetVendorTypeByIdAsync(int id)
    {
        return await _unitOfWork.VendorRepository.GetVendorTypeByIdAsync(id);
    }

    public async Task<VendorType> CreateVendorTypeAsync(VendorType vendorType)
    {
        var createdVendorType = await _unitOfWork.VendorRepository.CreateVendorTypeAsync(vendorType);
        await _unitOfWork.Complete();
        return createdVendorType;
    }

    public async Task<VendorType> UpdateVendorTypeAsync(VendorType vendorType)
    {
        var existingVendorType = await _unitOfWork.VendorRepository.GetVendorTypeByIdAsync(vendorType.Id);
        if (existingVendorType == null) 
            throw new ArgumentException($"Vendor type with id {vendorType.Id} not found");
        
        existingVendorType.Name = vendorType.Name;
        existingVendorType.Description = vendorType.Description;
        
        await _unitOfWork.Complete();
        return existingVendorType;
    }

    public async Task<bool> DeleteVendorTypeAsync(int id)
    {
        var result = await _unitOfWork.VendorRepository.DeleteVendorTypeAsync(id);
        await _unitOfWork.Complete();
        return result;
    }
}
