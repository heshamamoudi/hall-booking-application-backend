using AutoMapper;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Application.DTOs.Customer;
using HallApp.Application.DTOs.Auth;
using HallApp.Core.Entities;
using HallApp.Core.Entities.CustomerEntities;

namespace HallApp.Application.Services;

/// <summary>
/// Combined service for operations requiring both AppUser + Customer data
/// Used for profile management, customer dashboard, authentication-related operations
/// </summary>
public class CustomerProfileService : ICustomerProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICustomerService _customerService;
    private readonly IUserService _userService;

    public CustomerProfileService(
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        ICustomerService customerService,
        IUserService userService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _customerService = customerService;
        _userService = userService;
    }

    // Profile operations (combines AppUser + Customer)
    public async Task<(AppUser AppUser, Customer Customer)?> GetCustomerProfileAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return null;

        // Get AppUser data
        var appUser = await _userService.GetUserByIdAsync(userId);
        if (appUser == null) return null;

        // Get Customer data
        var customer = await _customerService.GetCustomerByAppUserIdAsync(userIdInt);
        if (customer == null) return null;

        return (appUser, customer);
    }

    public async Task<bool> UpdateCustomerProfileAsync(string userId, AppUser userData, Customer customerData)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return false;

        // Update AppUser data through UserService
        var appUser = await _userService.GetUserByIdAsync(userId);
        if (appUser == null) return false;

        appUser.FirstName = userData.FirstName;
        appUser.LastName = userData.LastName;
        appUser.PhoneNumber = userData.PhoneNumber;
        appUser.Updated = DateTime.UtcNow;

        await _userService.UpdateUserAsync(appUser);

        // Update Customer data through CustomerService
        var customer = await _customerService.GetCustomerByAppUserIdAsync(userIdInt);
        if (customer == null) return false;

        customer.CreditMoney = customerData.CreditMoney;
        customer.SelectedAddressId = customerData.SelectedAddressId;
        customer.Confirmed = customerData.Confirmed;

        await _customerService.UpdateCustomerAsync(customer);
        return true;
    }

    public async Task<bool> DeleteCustomerProfileAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return false;

        // Get customer first
        var customer = await _customerService.GetCustomerByAppUserIdAsync(userIdInt);
        if (customer == null) return false;

        // Delete customer business data
        await _customerService.DeleteCustomerAsync(customer.Id);

        // Delete AppUser through UserService (or deactivate)
        return await _userService.DeactivateUserAsync(userId);
    }

    // Authentication-related profile operations
    public async Task<bool> UpdatePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        return await _userService.ResetUserPasswordAsync(userId, newPassword);
    }

    public async Task<bool> VerifyEmailAsync(string userId, string token)
    {
        // This would typically verify the token and mark email as confirmed
        var appUser = await _userService.GetUserByIdAsync(userId);
        if (appUser == null) return false;

        appUser.EmailConfirmed = true;
        appUser.Updated = DateTime.UtcNow;
        await _userService.UpdateUserAsync(appUser);
        return true;
    }

    public async Task<bool> SendVerificationEmailAsync(string userId)
    {
        // Implementation would generate token and send email
        return await _userService.SendUserInvitationAsync(userId, "Customer");
    }

    // Business profile operations
    public async Task<(AppUser AppUser, Customer Customer)?> GetCustomerDashboardAsync(string userId)
    {
        var profile = await GetCustomerProfileAsync(userId);
        if (profile == null) return null;

        // Load additional dashboard data
        var customerWithRelationships = await _customerService.GetCustomerWithRelationshipsAsync(profile.Value.Customer.Id);
        if (customerWithRelationships != null)
        {
            return (profile.Value.AppUser, customerWithRelationships);
        }

        return profile;
    }

    public async Task<bool> UpdateBusinessPreferencesAsync(string userId, Customer businessData)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return false;

        var customer = await _customerService.GetCustomerByAppUserIdAsync(userIdInt);
        if (customer == null) return false;

        // Update business-specific preferences
        customer.CreditMoney = businessData.CreditMoney;
        customer.SelectedAddressId = businessData.SelectedAddressId;
        customer.Active = businessData.Active;
        customer.Confirmed = businessData.Confirmed;

        var updatedCustomer = await _customerService.UpdateCustomerAsync(customer);
        return updatedCustomer != null;
    }
}
