using AutoMapper;
using HallApp.Web.Controllers.Common;
using HallApp.Core.Exceptions;
using HallApp.Application.DTOs.Customer;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Interfaces;
using HallApp.Core.Entities;
using HallApp.Core.Entities.CustomerEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Customer
{
    /// <summary>
    /// Customer management controller
    /// Handles customer profile operations and admin customer management
    /// </summary>
    [Route("api/customers")]
    public class CustomerController : BaseApiController
    {
        private readonly ICustomerService _customerService;
        private readonly IAddressService _addressService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CustomerController(ICustomerService customerService, IAddressService addressService, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _customerService = customerService;
            _addressService = addressService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        /// <summary>
        /// Get all customers (Admin only)
        /// </summary>
        /// <returns>List of customers</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<CustomerDto>>>> GetCustomers()
        {
            try
            {
                var customerEntities = await _customerService.GetAllCustomersAsync();
                var customerDtos = _mapper.Map<IEnumerable<CustomerDto>>(customerEntities);
                return Success(customerDtos, "Customers retrieved successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<IEnumerable<CustomerDto>>
                {
                    StatusCode = 500,
                    Message = $"Failed to retrieve customers: {ex.Message}",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Get customer by ID
        /// </summary>
        /// <param name="id">Customer ID</param>
        /// <returns>Customer details</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<CustomerDto>>> GetCustomerById(int id)
        {
            try
            {
                // Admins can access any customer, customers can only access their own
                var customer = await _customerService.GetCustomerByIdAsync(id);

                if (customer == null)
                {
                    return Error<CustomerDto>("Customer not found", 404);
                }

                // If not admin, verify the customer is requesting their own data
                if (!IsAdmin)
                {
                    var requestingCustomer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                    if (requestingCustomer == null || requestingCustomer.Id != id)
                    {
                        return Error<CustomerDto>("Access denied", 403);
                    }
                }

                var customerDto = _mapper.Map<CustomerDto>(customer);
                return Success(customerDto, "Customer retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<CustomerDto>($"Failed to retrieve customer: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get current customer profile
        /// </summary>
        /// <returns>Current customer's profile</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse<CustomerDto>>> GetMyProfile()
        {
            try
            {
                var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                
                if (customer == null)
                {
                    return Error<CustomerDto>($"Customer with ID {UserId} not found", 404);
                }

                var customerDto = _mapper.Map<CustomerDto>(customer);
                return Success(customerDto, "Profile retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<CustomerDto>($"Failed to retrieve profile: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update customer profile
        /// </summary>
        /// <param name="updateDto">Updated customer information</param>
        /// <returns>Updated customer profile</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponse<CustomerDto>>> UpdateMyProfile([FromBody] CustomerDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Error<CustomerDto>($"Profile update failed: {ModelState.Values.FirstOrDefault().Errors.FirstOrDefault().ErrorMessage}", 400);
                }

                // Map DTO to entity for service call
                var customerEntity = _mapper.Map<HallApp.Core.Entities.CustomerEntities.Customer>(updateDto);
                
                // Get existing customer first
                var existingCustomer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                if (existingCustomer == null)
                {
                    return Error<CustomerDto>("Customer not found", 404);
                }
                
                // Update the existing customer with new data
                existingCustomer.CreditMoney = customerEntity.CreditMoney;
                existingCustomer.SelectedAddressId = customerEntity.SelectedAddressId;
                existingCustomer.Active = customerEntity.Active;
                existingCustomer.Confirmed = customerEntity.Confirmed;
                
                var updatedCustomer = await _customerService.UpdateCustomerAsync(existingCustomer);
                
                if (updatedCustomer == null)
                {
                    return Error<CustomerDto>("Failed to update profile", 500);
                }

                var customerDto = _mapper.Map<CustomerDto>(updatedCustomer);
                return Success(customerDto, "Profile updated successfully");
            }
            catch (Exception ex)
            {
                return Error<CustomerDto>($"Failed to update customer: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Delete customer account
        /// </summary>
        /// <param name="id">Customer ID</param>
        /// <returns>Success response</returns>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteCustomer(int id)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                
                if (!IsAdmin)
                {
                    return Error<string>("Access denied", 403);
                }

                var result = await _customerService.DeleteCustomerAsync(id);
                
                if (!result)
                {
                    return Error<string>("Failed to delete customer", 500);
                }

                return Success<string>("Customer deleted successfully", "Customer deleted successfully");
            }
            catch (Exception ex)
            {
                return Error<string>($"Failed to delete customer: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Toggle customer active status (Admin only)
        /// </summary>
        /// <param name="id">Customer ID</param>
        /// <param name="active">Active status</param>
        /// <returns>Updated customer</returns>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/toggle-active")]
        public async Task<ActionResult<ApiResponse<CustomerDto>>> ToggleCustomerActive(int id, [FromQuery] bool active)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                
                if (customer == null)
                {
                    return Error<CustomerDto>($"Customer with ID {id} not found", 404);
                }

                customer.Active = active;
                var updatedCustomer = await _customerService.UpdateCustomerAsync(customer);
                
                if (updatedCustomer == null)
                {
                    return Error<CustomerDto>("Failed to update customer status", 500);
                }

                var customerDto = _mapper.Map<CustomerDto>(updatedCustomer);
                return Success(customerDto, $"Customer {(active ? "activated" : "deactivated")} successfully");
            }
            catch (Exception ex)
            {
                return Error<CustomerDto>($"Failed to update customer: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Test API connection
        /// </summary>
        /// <returns>API status</returns>
        [HttpGet("test")]
        [AllowAnonymous]
        public ActionResult<ApiResponse<object>> TestApiConnection()
        {
            object status = new
            {
                message = "Customer API is operational",
                timestamp = DateTime.UtcNow,
                version = "1.0"
            };
            return Success(status, "API Status");
        }

        /// <summary>
        /// Get all addresses for a customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>List of customer addresses</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpGet("{customerId:int}/addresses")]
        public async Task<ActionResult<ApiResponse<IEnumerable<AddressDto>>>> GetCustomerAddresses(int customerId)
        {
            try
            {
                // Admins can access any customer's addresses
                if (!IsAdmin)
                {
                    // Verify customer ownership for non-admin users
                    var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                    if (customer == null || customer.Id != customerId)
                    {
                        return Error<IEnumerable<AddressDto>>("Access denied", 403);
                    }
                }

                var addresses = await _addressService.GetAddressesByCustomerIdAsync(customerId);
                var addressDtos = _mapper.Map<IEnumerable<AddressDto>>(addresses);
                return Success(addressDtos, "Addresses retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<AddressDto>>($"Failed to retrieve addresses: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Add a new address for a customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="addressDto">Address data</param>
        /// <returns>Created address</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpPost("{customerId:int}/addresses")]
        public async Task<ActionResult<ApiResponse<AddressDto>>> CreateAddress(int customerId, [FromBody] AddressDto addressDto)
        {
            try
            {
                // Admins can create addresses for any customer
                if (!IsAdmin)
                {
                    // Verify customer ownership for non-admin users
                    var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                    if (customer == null || customer.Id != customerId)
                    {
                        return Error<AddressDto>("Access denied", 403);
                    }
                }

                if (!ModelState.IsValid)
                {
                    return Error<AddressDto>("Invalid address data", 400);
                }

                var address = _mapper.Map<Address>(addressDto);
                address.CustomerId = customerId;
                address.Id = 0; // Ensure new entity

                var createdAddress = await _addressService.CreateAddressAsync(address);
                var createdAddressDto = _mapper.Map<AddressDto>(createdAddress);
                return Success(createdAddressDto, "Address created successfully");
            }
            catch (Exception ex)
            {
                return Error<AddressDto>($"Failed to create address: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update an address for a customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="addressId">Address ID</param>
        /// <param name="addressDto">Updated address data</param>
        /// <returns>Updated address</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpPut("{customerId:int}/addresses/{addressId:int}")]
        public async Task<ActionResult<ApiResponse<AddressDto>>> UpdateAddress(int customerId, int addressId, [FromBody] AddressDto addressDto)
        {
            try
            {
                // Admins can update addresses for any customer
                if (!IsAdmin)
                {
                    // Verify customer ownership for non-admin users
                    var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                    if (customer == null || customer.Id != customerId)
                    {
                        return Error<AddressDto>("Access denied", 403);
                    }
                }

                var addresses = await _addressService.GetAddressesByCustomerIdAsync(customerId);
                var existingAddress = addresses.FirstOrDefault(a => a.Id == addressId);

                if (existingAddress == null)
                {
                    return Error<AddressDto>("Address not found", 404);
                }

                // Update address properties
                existingAddress.Street = addressDto.Street1 ?? existingAddress.Street;
                existingAddress.City = addressDto.City ?? existingAddress.City;
                existingAddress.State = addressDto.Street2 ?? existingAddress.State;
                existingAddress.ZipCode = addressDto.ZipCode.ToString();

                // Handle main address setting
                if (addressDto.IsMain && !existingAddress.IsMain)
                {
                    await _addressService.SetMainAddressAsync(customerId, addressId);
                }

                var updatedAddress = await _addressService.UpdateAddressAsync(existingAddress);
                var updatedAddressDto = _mapper.Map<AddressDto>(updatedAddress);
                return Success(updatedAddressDto, "Address updated successfully");
            }
            catch (Exception ex)
            {
                return Error<AddressDto>($"Failed to update address: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Delete an address for a customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="addressId">Address ID</param>
        /// <returns>Success response</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpDelete("{customerId:int}/addresses/{addressId:int}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteAddress(int customerId, int addressId)
        {
            try
            {
                // Admins can delete addresses for any customer
                if (!IsAdmin)
                {
                    // Verify customer ownership for non-admin users
                    var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                    if (customer == null || customer.Id != customerId)
                    {
                        return Error<string>("Access denied", 403);
                    }
                }

                var result = await _addressService.DeleteAddressAsync(customerId, addressId);
                if (!result)
                {
                    return Error<string>("Cannot delete address", 400);
                }
                return Success<string>("Address deleted successfully", "Address deleted successfully");
            }
            catch (Exception ex)
            {
                return Error<string>($"Failed to delete address: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get all favorite halls for a customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>List of favorite halls</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpGet("{customerId:int}/favorites")]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetFavorites(int customerId)
        {
            try
            {
                // Admins can access any customer's favorites
                if (!IsAdmin)
                {
                    // Verify customer ownership for non-admin users
                    var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                    if (customer == null || customer.Id != customerId)
                    {
                        return Error<IEnumerable<object>>("Access denied", 403);
                    }
                }

                var favorites = await _unitOfWork.FavoriteRepository.GetFavoritesByCustomerIdAsync(customerId);

                // Map to simple DTOs to avoid circular reference issues
                var favoriteDtos = favorites.Select(f => new
                {
                    id = f.Id,
                    customerId = f.CustomerId,
                    hallId = f.HallId,
                    created = f.CreatedAt,
                    updated = f.CreatedAt
                }).ToList();

                return Success<IEnumerable<object>>(favoriteDtos, "Favorites retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<object>>($"Failed to retrieve favorites: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Add a hall to favorites
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="favoriteDto">Favorite data containing hallId</param>
        /// <returns>Success response</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpPost("{customerId:int}/favorites")]
        public async Task<ActionResult<ApiResponse<string>>> AddFavorite(int customerId, [FromBody] CreateFavoriteDto favoriteDto)
        {
            try
            {
                // Admins can add favorites for any customer
                if (!IsAdmin)
                {
                    // Verify customer ownership for non-admin users
                    var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                    if (customer == null || customer.Id != customerId)
                    {
                        return Error<string>("Access denied", 403);
                    }
                }

                if (favoriteDto == null || !favoriteDto.HallId.HasValue || favoriteDto.HallId.Value <= 0)
                {
                    return Error<string>("Invalid hall ID", 400);
                }

                var hallId = favoriteDto.HallId.Value;

                // Check if favorite already exists
                var exists = await _unitOfWork.FavoriteRepository.FavoriteExistsAsync(customerId, hallId);
                if (exists)
                {
                    return Error<string>("Hall is already in favorites", 400);
                }

                await _unitOfWork.FavoriteRepository.AddFavoriteAsync(customerId, hallId);
                await _unitOfWork.Complete();

                return Success<string>("Favorite added successfully", "Favorite added successfully");
            }
            catch (Exception ex)
            {
                return Error<string>($"Failed to add favorite: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Remove a hall from favorites
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="hallId">Hall ID to remove</param>
        /// <returns>Success response</returns>
        [Authorize(Roles = "Admin,Customer")]
        [HttpDelete("{customerId:int}/favorites/{hallId:int}")]
        public async Task<ActionResult<ApiResponse<string>>> RemoveFavorite(int customerId, int hallId)
        {
            try
            {
                // Admins can remove favorites for any customer
                if (!IsAdmin)
                {
                    // Verify customer ownership for non-admin users
                    var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                    if (customer == null || customer.Id != customerId)
                    {
                        return Error<string>("Access denied", 403);
                    }
                }

                var removed = await _unitOfWork.FavoriteRepository.RemoveFavoriteAsync(customerId, hallId);
                if (!removed)
                {
                    return Error<string>("Favorite not found", 404);
                }

                await _unitOfWork.Complete();
                return Success<string>("Favorite removed successfully", "Favorite removed successfully");
            }
            catch (Exception ex)
            {
                return Error<string>($"Failed to remove favorite: {ex.Message}", 500);
            }
        }
    }
}
