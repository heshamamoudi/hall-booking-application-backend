using HallApp.Core.Entities.CustomerEntities;

namespace HallApp.Core.Interfaces.IServices
{
    /// <summary>
    /// Pure business domain service for Customer entity operations
    /// Focuses only on business logic - no AppUser/authentication concerns
    /// </summary>
    public interface ICustomerService
    {
        // Core CRUD operations
        Task<Customer> GetCustomerByIdAsync(int customerId);
        Task<Customer> GetCustomerByAppUserIdAsync(int appUserId);
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(int customerId);
        
        // Business operations
        Task<bool> UpdateCustomerCreditAsync(int customerId, int creditAmount);
        Task<bool> IncrementOrderCountAsync(int customerId);
        Task<bool> SetSelectedAddressAsync(int customerId, int addressId);
        Task<bool> ToggleCustomerStatusAsync(int customerId);
        
        // Business queries
        Task<IEnumerable<Customer>> GetAllCustomersAsync();
        Task<IEnumerable<Customer>> GetActiveCustomersAsync();
        Task<IEnumerable<Customer>> GetRecentCustomersAsync(int count);
        Task<int> GetCustomerBookingCountAsync(int customerId);
        Task<bool> CustomerExistsAsync(int customerId);
        
        // Business relationships
        Task<IEnumerable<Customer>> GetCustomersWithBookingsAsync();
        Task<IEnumerable<Customer>> GetCustomersWithReviewsAsync();
        Task<Customer> GetCustomerWithRelationshipsAsync(int customerId);
        
        // Business validation
        Task<bool> ValidateCustomerAsync(Customer customer);
        Task<bool> CanCustomerBookAsync(int customerId);
        Task<bool> HasSufficientCreditAsync(int customerId, int requiredAmount);
    }
}
