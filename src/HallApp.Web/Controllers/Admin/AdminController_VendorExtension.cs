using HallApp.Web.Controllers.Common;
using HallApp.Core.Exceptions;
using HallApp.Application.DTOs.Vendors;
using HallApp.Application.DTOs.Halls.HallManager;
using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;

namespace HallApp.Web.Controllers.Admin
{
    /// <summary>
    /// Vendor Manager Extension for AdminController
    /// Provides vendor manager management for Admins
    /// NOTE: Vendor CRUD operations moved to VendorController (/api/vendors)
    /// </summary>
    public partial class AdminController
    {
        // NOTE: Vendor CRUD operations moved to VendorController (/api/vendors)
        // Use /api/vendors for all vendor operations (GET, POST, PUT, DELETE)
        // Admin can still access vendors via VendorController with Admin role

        // Vendor Manager Management Methods

        /// <summary>
        /// Get all vendor managers
        /// </summary>
        /// <returns>List of vendor managers</returns>
        [HttpGet("vendor-managers")]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetVendorManagers()
        {
            try
            {
                var vendorManagers = await _vendorManagerService.GetAllVendorManagersAsync();
                var vendorManagerDtos = vendorManagers.Select(vm => new
                {
                    id = vm.Id,
                    appUserId = vm.AppUserId,
                    createdAt = vm.CreatedAt,
                    userName = vm.AppUser?.UserName ?? "",
                    email = vm.AppUser?.Email ?? "",
                    firstName = vm.AppUser?.FirstName ?? "",
                    lastName = vm.AppUser?.LastName ?? "",
                    phoneNumber = vm.AppUser?.PhoneNumber ?? ""
                });

                return Success<IEnumerable<object>>(vendorManagerDtos, "Vendor managers retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<object>>($"Failed to retrieve vendor managers: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Create vendor manager
        /// </summary>
        /// <param name="userCreateDto">Vendor manager creation data</param>
        /// <returns>Created vendor manager</returns>
        [HttpPost("vendor-managers")]
        public async Task<ActionResult<ApiResponse<object>>> CreateVendorManager([FromBody] UserCreateDto userCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<object>($"Invalid data: {errors}", 400);
                }

                // Create AppUser first
                var userEntity = _mapper.Map<HallApp.Core.Entities.AppUser>(userCreateDto);
                var user = await _userService.CreateUserAsync(userEntity);

                if (user != null)
                {
                    // Create VendorManager business entity
                    var vendorManager = new HallApp.Core.Entities.VendorEntities.VendorManager
                    {
                        AppUserId = user.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createdVendorManager = await _vendorManagerService.CreateVendorManagerAsync(vendorManager);

                    var result = new
                    {
                        id = createdVendorManager.Id,
                        appUserId = createdVendorManager.AppUserId,
                        createdAt = createdVendorManager.CreatedAt,
                        userName = user.UserName,
                        email = user.Email
                    };

                    return Success((object)result, "Vendor manager created successfully");
                }

                return Error<object>("Couldn't create vendor manager", 500);
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to create vendor manager: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Assign vendor to vendor manager
        /// Creates a relationship between vendor manager and vendor
        /// </summary>
        /// <param name="managerId">Vendor manager ID</param>
        /// <param name="vendorId">Vendor ID to assign</param>
        /// <returns>Success response</returns>
        [HttpPost("vendor-managers/{managerId:int}/assign-vendor/{vendorId:int}")]
        public async Task<ActionResult<ApiResponse>> AssignVendorToManager(int managerId, int vendorId)
        {
            try
            {
                var vendorManager = await _vendorManagerService.GetVendorManagerByIdAsync(managerId);
                if (vendorManager == null)
                {
                    return Error($"Vendor manager with ID {managerId} not found", 404);
                }

                var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
                if (vendor == null)
                {
                    return Error($"Vendor with ID {vendorId} not found", 404);
                }

                // Check if already assigned
                if (vendorManager.Vendors.Any(v => v.Id == vendorId))
                {
                    return Error($"Vendor {vendorId} is already assigned to manager {managerId}", 400);
                }

                // Add vendor to manager's collection
                vendorManager.Vendors.Add(vendor);
                await _vendorManagerService.UpdateVendorManagerAsync(vendorManager);

                return Success($"Vendor '{vendor.Name}' successfully assigned to manager {managerId}");
            }
            catch (Exception ex)
            {
                return Error($"Failed to assign vendor: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Unassign vendor from vendor manager
        /// Removes the relationship between vendor manager and vendor
        /// </summary>
        /// <param name="managerId">Vendor manager ID</param>
        /// <param name="vendorId">Vendor ID to unassign</param>
        /// <returns>Success response</returns>
        [HttpDelete("vendor-managers/{managerId:int}/unassign-vendor/{vendorId:int}")]
        public async Task<ActionResult<ApiResponse>> UnassignVendorFromManager(int managerId, int vendorId)
        {
            try
            {
                var vendorManager = await _vendorManagerService.GetVendorManagerByIdAsync(managerId);
                if (vendorManager == null)
                {
                    return Error($"Vendor manager with ID {managerId} not found", 404);
                }

                var vendor = vendorManager.Vendors.FirstOrDefault(v => v.Id == vendorId);
                if (vendor == null)
                {
                    return Error($"Vendor {vendorId} is not assigned to manager {managerId}", 400);
                }

                // Remove vendor from manager's collection
                vendorManager.Vendors.Remove(vendor);
                await _vendorManagerService.UpdateVendorManagerAsync(vendorManager);

                return Success($"Vendor '{vendor.Name}' successfully unassigned from manager {managerId}");
            }
            catch (Exception ex)
            {
                return Error($"Failed to unassign vendor: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Delete vendor manager
        /// </summary>
        /// <param name="id">Vendor manager ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("vendor-managers/{id:int}")]
        public async Task<ActionResult<ApiResponse>> DeleteVendorManager(int id)
        {
            try
            {
                var result = await _vendorManagerService.DeleteVendorManagerAsync(id);
                if (!result)
                {
                    return Error($"Vendor manager with ID {id} not found", 404);
                }

                return Success("Vendor manager deleted successfully");
            }
            catch (Exception ex)
            {
                return Error($"Failed to delete vendor manager: {ex.Message}", 500);
            }
        }
    }
}
