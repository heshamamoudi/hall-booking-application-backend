using HallApp.Core.Entities.CustomerEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IAddressRepository : IGenericRepository<Address>
{
    Task<IEnumerable<Address>> GetAddressesByCustomerIdAsync(int customerId);
    Task<Address> GetMainAddressByCustomerIdAsync(int customerId);
    Task<bool> DeleteByCustomerIdAsync(int customerId, int addressId);
    Task<bool> SetMainAddressAsync(int customerId, int addressId);
}
