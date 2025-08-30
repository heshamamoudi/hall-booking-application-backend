using AutoMapper;
using HallApp.Web.Controllers.Common;
using HallApp.Application.Common.Models;
using HallApp.Application.DTOs.Customer;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Customer
{
    /// <summary>
    /// Customer management controller
    /// Handles customer profile operations and admin customer management
    /// </summary>
    [Route("api/v1/customers")]
    public class CustomerController : BaseApiController
    {
        private readonly ICustomerService _customerService;
        private readonly IMapper _mapper;

        public CustomerController(ICustomerService customerService, IMapper mapper)
        {
            _customerService = customerService;
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
        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<CustomerDto>>> GetCustomerById(int id)
        {
            try
            {
                if (!IsCustomer || UserId != id)
                {
                    return Error<CustomerDto>("Access denied", 403);
                }

                var customer = await _customerService.GetCustomerByAppUserIdAsync(UserId);
                
                if (customer == null)
                {
                    return Error<CustomerDto>("Customer not found", 404);
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
        [Authorize(Roles = "Customer")]
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
        [Authorize(Roles = "Customer")]
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
    }
}
