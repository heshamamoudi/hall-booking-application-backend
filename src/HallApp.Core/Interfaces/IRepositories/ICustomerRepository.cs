using HallApp.Core.Entities.CustomerEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface ICustomerRepository : IGenericRepository<Customer>
{
    Task<Customer> GetCustomerByAppUserIdAsync(int appUserId);
    Task<Customer> GetCustomerWithAddressesAsync(int customerId);
    Task<Customer> GetCustomerWithBookingsAsync(int customerId);
    Task<IEnumerable<Customer>> GetCustomersWithOrdersAsync();
}
