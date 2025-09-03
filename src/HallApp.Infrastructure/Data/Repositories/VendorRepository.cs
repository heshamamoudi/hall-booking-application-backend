using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Application.Common;
using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class VendorRepository : GenericRepository<Vendor>, IVendorRepository
{
    public VendorRepository(DataContext context) : base(context)
    {
    }

    public async Task<Vendor> CreateVendor(Vendor vendor)
    {
        await AddAsync(vendor);
        await SaveAllAsync();
        return vendor;
    }

    public async Task<IEnumerable<Vendor>> GetAllVendorsAsync()
    {
        return await _context.Vendors
            .Include(v => v.VendorType)
            .Include(v => v.Location)
            .Include(v => v.BusinessHours)
            .ToListAsync();
    }


    public async Task<Vendor> GetVendorByIdAsync(int id)
    {
        return await _context.Vendors
            .Include(v => v.VendorType)
            .Include(v => v.Location)
            .Include(v => v.BusinessHours)
            .Include(v => v.ServiceItems)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<List<Vendor>> GetVendorsByTypeAsync(int typeId)
    {
        return await _context.Vendors
            .Include(v => v.VendorType)
            .Include(v => v.Location)
            .Where(v => v.VendorTypeId == typeId)
            .ToListAsync();
    }

    public async Task<List<Vendor>> GetVendorsByManagerAsync(int managerId)
    {
        return await _context.Vendors
            .Include(v => v.VendorType)
            .Include(v => v.Location)
            .Include(v => v.Managers.Where(m => m.Id == managerId))
            .Where(v => v.Managers.Any(m => m.Id == managerId))
            .ToListAsync();
    }

    public async Task<List<Vendor>> SearchVendorsAsync(string searchTerm)
    {
        return await _context.Vendors
            .Include(v => v.VendorType)
            .Include(v => v.Location)
            .Where(v => v.Name.ToLower().Contains(searchTerm.ToLower()))
            .ToListAsync();
    }

    public async Task<Vendor> UpdateVendorAsync(int id, Vendor updateVendor)
    {
        var vendor = await GetVendorByIdAsync(id);
        if (vendor == null)
            throw new Exception("Vendor not found");

        vendor.Name = updateVendor.Name;
        vendor.Description = updateVendor.Description;
        vendor.Email = updateVendor.Email;
        vendor.Phone = updateVendor.Phone;
        vendor.IsActive = updateVendor.IsActive;
        vendor.VendorTypeId = updateVendor.VendorTypeId;

        Update(vendor);
        await SaveAllAsync();
        return vendor;
    }

    public async Task<VendorLocation> UpdateVendorLocationAsync(int vendorId, VendorLocation updateLocation)
    {
        var vendor = await GetVendorByIdAsync(vendorId);
        if (vendor == null)
            throw new Exception("Vendor not found");

        var location = vendor.Location;
        if (location == null)
        {
            location = new VendorLocation { VendorId = vendorId };
            vendor.Location = location;
        }

        location.Address = updateLocation.Address;
        location.City = updateLocation.City;
        location.State = updateLocation.State;
        location.Country = updateLocation.Country;
        location.PostalCode = updateLocation.PostalCode;
        location.Latitude = updateLocation.Latitude;
        location.Longitude = updateLocation.Longitude;

        await SaveAllAsync();
        return location;
    }

    public async Task<bool> DeleteVendorAsync(int id)
    {
        var vendor = await GetByIdAsync(id);
        if (vendor == null)
            throw new Exception("Vendor not found");

        Delete(vendor);
        return await SaveAllAsync();
    }

    public async Task<bool> IsNameUniqueAsync(string name, int excludeId = 0)
    {
        var query = _context.Vendors.AsQueryable();
        if (excludeId != 0)
        {
            query = query.Where(v => v.Id != excludeId);
        }
        return !await query.AnyAsync(v => v.Name.ToLower() == name.ToLower());
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int excludeId = 0)
    {
        var query = _context.Vendors.AsQueryable();
        if (excludeId != 0)
        {
            query = query.Where(v => v.Id != excludeId);
        }
        return !await query.AnyAsync(v => v.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int excludeId = 0)
    {
        if (string.IsNullOrEmpty(phone))
            return false;

        var query = _context.Vendors.AsQueryable();

        if (excludeId != 0)
            query = query.Where(v => v.Id != excludeId);

        return !await query.AnyAsync(v => v.Phone == phone);
    }

    public async Task<List<VendorType>> GetVendorTypesAsync()
    {
        return await _context.VendorTypes
            .OrderBy(vt => vt.Name)
            .ToListAsync();
    }

    public async Task<VendorType> GetVendorTypeByIdAsync(int id)
    {
        return await _context.VendorTypes.FindAsync(id);
    }

    public Task<VendorType> CreateVendorTypeAsync(VendorType vendorType)
    {
        _context.VendorTypes.Add(vendorType);
        return Task.FromResult(vendorType);
    }

    public Task<VendorType> UpdateVendorTypeAsync(VendorType vendorType)
    {
        _context.VendorTypes.Update(vendorType);
        return Task.FromResult(vendorType);
    }

    public Task<bool> DeleteVendorTypeAsync(int id)
    {
        var vendorType = _context.VendorTypes.Find(id);
        if (vendorType == null)
            return Task.FromResult(false);

        _context.VendorTypes.Remove(vendorType);
        return Task.FromResult(true);
    }
}
