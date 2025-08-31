using HallApp.Core.Entities.CustomerEntities;

namespace HallApp.Core.Interfaces.IServices;

public interface IAddressService
{
    Task<IEnumerable<Address>> GetAddressesByCustomerIdAsync(int customerId);
    Task<Address?> GetAddressByIdAsync(int addressId);
    Task<Address?> GetMainAddressByCustomerIdAsync(int customerId);
    Task<Address> CreateAddressAsync(Address address);
    Task<Address?> UpdateAddressAsync(Address address);
    Task<bool> DeleteAddressAsync(int customerId, int addressId);
    Task<bool> SetMainAddressAsync(int customerId, int addressId);
}
