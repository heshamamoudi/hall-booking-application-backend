using AutoMapper;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities.CustomerEntities;

namespace HallApp.Application.Services;

/// <summary>
/// Pure business domain service for Customer entity operations
/// Handles only business logic - no AppUser/authentication concerns
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CustomerService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // Core CRUD operations
    public async Task<Customer> GetCustomerByIdAsync(int customerId)
    {
        return await _unitOfWork.CustomerRepository.GetByIdAsync(customerId) ?? new Customer();
    }

    public async Task<Customer> GetCustomerByAppUserIdAsync(int appUserId)
    {
        var allCustomers = await _unitOfWork.CustomerRepository.GetAllAsync();
        return allCustomers.FirstOrDefault(c => c.AppUserId == appUserId) ?? new Customer();
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        customer.Created = DateTime.UtcNow;
        customer.Updated = DateTime.UtcNow;
        customer.Active = true;
        customer.Confirmed = false;
        customer.NumberOfOrders = 0;
        customer.CreditMoney = Math.Max(0, customer.CreditMoney);
        
        await _unitOfWork.CustomerRepository.AddAsync(customer);
        await _unitOfWork.Complete();
        return customer;
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        var existingCustomer = await _unitOfWork.CustomerRepository.GetByIdAsync(customer.Id);
        if (existingCustomer == null) return new Customer();
        
        // Update business fields only
        existingCustomer.NumberOfOrders = customer.NumberOfOrders;
        existingCustomer.SelectedAddressId = customer.SelectedAddressId;
        existingCustomer.CreditMoney = Math.Max(0, customer.CreditMoney);
        existingCustomer.Confirmed = customer.Confirmed;
        existingCustomer.Active = customer.Active;
        existingCustomer.Updated = DateTime.UtcNow;
        
        _unitOfWork.CustomerRepository.Update(existingCustomer);
        await _unitOfWork.Complete();
        return existingCustomer;
    }

    public async Task<bool> DeleteCustomerAsync(int customerId)
    {
        var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(customerId);
        if (customer == null) return false;
        
        _unitOfWork.CustomerRepository.Delete(customer);
        await _unitOfWork.Complete();
        return true;
    }

    // Business operations
    public async Task<bool> UpdateCustomerCreditAsync(int customerId, int creditAmount)
    {
        var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(customerId);
        if (customer == null) return false;
        
        customer.CreditMoney = Math.Max(0, creditAmount);
        customer.Updated = DateTime.UtcNow;
        _unitOfWork.CustomerRepository.Update(customer);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> IncrementOrderCountAsync(int customerId)
    {
        var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(customerId);
        if (customer == null) return false;
        
        customer.NumberOfOrders++;
        customer.Updated = DateTime.UtcNow;
        _unitOfWork.CustomerRepository.Update(customer);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> SetSelectedAddressAsync(int customerId, int addressId)
    {
        var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(customerId);
        if (customer == null) return false;
        
        customer.SelectedAddressId = addressId;
        customer.Updated = DateTime.UtcNow;
        _unitOfWork.CustomerRepository.Update(customer);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> ToggleCustomerStatusAsync(int customerId)
    {
        var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(customerId);
        if (customer == null) return false;
        
        customer.Active = !customer.Active;
        customer.Updated = DateTime.UtcNow;
        _unitOfWork.CustomerRepository.Update(customer);
        await _unitOfWork.Complete();
        return true;
    }

    // Business queries
    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        return await _unitOfWork.CustomerRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        var allCustomers = await _unitOfWork.CustomerRepository.GetAllAsync();
        return allCustomers.Where(c => c.Active).ToList();
    }

    public async Task<IEnumerable<Customer>> GetRecentCustomersAsync(int count)
    {
        var allCustomers = await _unitOfWork.CustomerRepository.GetAllAsync();
        return allCustomers.OrderByDescending(c => c.Created)
                          .Take(count)
                          .ToList();
    }

    public async Task<int> GetCustomerBookingCountAsync(int customerId)
    {
        var bookings = await _unitOfWork.BookingRepository.GetBookingsByCustomerIdAsync(customerId);
        return bookings.Count();
    }

    public async Task<bool> CustomerExistsAsync(int customerId)
    {
        var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(customerId);
        return customer != null;
    }

    // Business relationships
    public async Task<IEnumerable<Customer>> GetCustomersWithBookingsAsync()
    {
        var allCustomers = await _unitOfWork.CustomerRepository.GetAllAsync();
        var customersWithBookings = new List<Customer>();
        
        foreach (var customer in allCustomers)
        {
            var bookings = await _unitOfWork.BookingRepository.GetBookingsByCustomerIdAsync(customer.Id);
            if (bookings.Any())
            {
                customer.Bookings = bookings.ToList();
                customersWithBookings.Add(customer);
            }
        }
        return customersWithBookings;
    }

    public async Task<IEnumerable<Customer>> GetCustomersWithReviewsAsync()
    {
        var allCustomers = await _unitOfWork.CustomerRepository.GetAllAsync();
        var customersWithReviews = new List<Customer>();
        
        foreach (var customer in allCustomers)
        {
            var reviews = await _unitOfWork.ReviewRepository.GetReviewsByCustomerIdAsync(customer.Id);
            if (reviews.Any())
            {
                customer.Reviews = reviews.ToList();
                customersWithReviews.Add(customer);
            }
        }
        return customersWithReviews;
    }

    public async Task<Customer> GetCustomerWithRelationshipsAsync(int customerId)
    {
        var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(customerId);
        if (customer == null) return new Customer();
        
        // Load all business relationships
        customer.Bookings = (await _unitOfWork.BookingRepository.GetBookingsByCustomerIdAsync(customerId)).ToList();
        customer.Reviews = (await _unitOfWork.ReviewRepository.GetReviewsByCustomerIdAsync(customerId)).ToList();
        // customer.Addresses would be loaded here if repository method exists
        
        return customer;
    }

    // Business validation
    public Task<bool> ValidateCustomerAsync(Customer customer)
    {
        if (customer == null) return Task.FromResult(false);
        if (customer.AppUserId <= 0) return Task.FromResult(false);
        if (customer.CreditMoney < 0) return Task.FromResult(false);
        
        return Task.FromResult(true);
    }

    public async Task<bool> CanCustomerBookAsync(int customerId)
    {
        var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(customerId);
        if (customer == null) return false;
        
        return customer.Active && customer.Confirmed;
    }

    public async Task<bool> HasSufficientCreditAsync(int customerId, int requiredAmount)
    {
        var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(customerId);
        if (customer == null) return false;
        
        return customer.CreditMoney >= requiredAmount;
    }
}
