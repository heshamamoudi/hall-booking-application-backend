using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class AddressRepository : GenericRepository<Address>, IAddressRepository
{
    public AddressRepository(DataContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Address>> GetAddressesByCustomerIdAsync(int customerId)
    {
        return await _context.Addresses
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsMain)
            .ThenBy(a => a.Street)
            .ToListAsync();
    }

    public async Task<IEnumerable<Address>> GetAddressesByCustomerIdAsync(int customerId, string searchTerm)
    {
        return await _context.Addresses
            .Where(a => a.CustomerId == customerId && (a.Street.Contains(searchTerm) || a.City.Contains(searchTerm)))
            .OrderByDescending(a => a.IsMain)
            .ThenBy(a => a.Street)
            .ToListAsync();
    }

    public async Task<Address> GetMainAddressByCustomerIdAsync(int customerId)
    {
        return await _context.Addresses
            .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.IsMain);
    }

    public async Task<bool> DeleteByCustomerIdAsync(int customerId, int addressId)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId);

        if (address == null)
            return false;

        // Don't allow deletion of main address if it's the only one
        if (address.IsMain)
        {
            var addressCount = await _context.Addresses
                .CountAsync(a => a.CustomerId == customerId);
            
            if (addressCount == 1)
                return false; // Can't delete the only address
        }

        _context.Addresses.Remove(address);
        return true;
    }

    public async Task<bool> SetMainAddressAsync(int customerId, int addressId)
    {
        var targetAddress = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId);

        if (targetAddress == null)
            return false;

        // Reset all addresses for this customer to not be main
        var customerAddresses = await _context.Addresses
            .Where(a => a.CustomerId == customerId)
            .ToListAsync();

        foreach (var address in customerAddresses)
        {
            address.IsMain = address.Id == addressId;
        }

        return true;
    }
}
