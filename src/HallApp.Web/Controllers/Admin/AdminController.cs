using HallApp.Web.Controllers.Common;
using HallApp.Web.Services;
using HallApp.Web.DTOs;
using HallApp.Core.Exceptions;
using HallApp.Application.DTOs.Admin;
using HallApp.Application.DTOs.Halls.Hall;
using HallApp.Application.DTOs.Halls.HallManager;
using HallApp.Application.DTOs.Halls.HallManager;
using HallApp.Application.DTOs.User;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities;
using HallApp.Core.Entities.ChamperEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace HallApp.Web.Controllers.Admin
{
    /// <summary>
    /// Admin management controller
    /// Handles user management, role assignment, and system administration
    /// Extended with vendor and booking management
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/admin")]
    public partial class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUserService _userService;
        private readonly IHallService _hallService;
        private readonly IVendorService _vendorService;
        private readonly IHallManagerService _hallManagerService;
        private readonly IHallManagerProfileService _hallManagerProfileService;
        private readonly IVendorManagerService _vendorManagerService;
        private readonly IVendorProfileService _vendorProfileService;
        private readonly IBookingService _bookingService;
        private readonly IMapper _mapper;
        private readonly IFileUploadService _fileUploadService;

        public AdminController(
            IUserService userService,
            IHallService hallService,
            IVendorService vendorService,
            IHallManagerService hallManagerService,
            IHallManagerProfileService hallManagerProfileService,
            IVendorManagerService vendorManagerService,
            IVendorProfileService vendorProfileService,
            IBookingService bookingService,
            IMapper mapper,
            UserManager<HallApp.Core.Entities.AppUser> userManager,
            IFileUploadService fileUploadService)
        {
            _userService = userService;
            _hallService = hallService;
            _vendorService = vendorService;
            _hallManagerService = hallManagerService;
            _hallManagerProfileService = hallManagerProfileService;
            _vendorManagerService = vendorManagerService;
            _vendorProfileService = vendorProfileService;
            _bookingService = bookingService;
            _mapper = mapper;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
        }

        /// <summary>
        /// Get all users with their roles
        /// </summary>
        /// <returns>List of users with roles</returns>
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UsersDto>>>> GetUsersWithRoles()
        {
            try
            {
                var users = await _userManager.Users
                    .OrderBy(u => u.UserName)
                    .Select(u => new UsersDto
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        EmailConfirmed = u.EmailConfirmed,
                        PhoneNumber = u.PhoneNumber,
                        DOB = u.DOB,
                        Created = u.Created,
                        Updated = u.Updated,
                        Roles = u.UserRoles.Select(r => r.Role.Name).ToList(),
                        Active = u.Active
                    }).ToListAsync();

                return Success<IEnumerable<UsersDto>>(users, "Users with roles retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<UsersDto>>($"Failed to retrieve users: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update user roles
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="roles">Comma-separated roles</param>
        /// <returns>Updated user roles</returns>
        [HttpPut("users/{id}/roles")]
        public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> EditUserRoles(string id, [FromQuery] string roles)
        {
            try
            {
                if (string.IsNullOrEmpty(roles))
                {
                    return Error<IEnumerable<string>>("No roles provided", 400);
                }

                var selectedRoles = roles.Split(",").ToArray();
                var user = await _userManager.FindByIdAsync(id);
                
                if (user == null)
                {
                    return Error<IEnumerable<string>>($"User with ID {id} not found", 404);
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                
                // Add new roles
                var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
                if (!result.Succeeded)
                {
                    return Error<IEnumerable<string>>("Failed to add roles", 400);
                }

                // Remove old roles
                result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
                if (!result.Succeeded)
                {
                    return Error<IEnumerable<string>>($"Failed to remove roles: {string.Join(", ", result.Errors.Select(e => e.Description))}", 400);
                }

                var updatedRoles = await _userManager.GetRolesAsync(user);
                return Success<IEnumerable<string>>(updatedRoles.ToList(), "User roles updated successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<string>>($"Failed to update user roles: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update user profile (Admin/HallManager users only)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="userUpdateDto">Updated user data</param>
        /// <returns>Updated user with roles</returns>
        [HttpPut("users/{id}")]
        public async Task<ActionResult<ApiResponse<UsersDto>>> UpdateUser(string id, [FromBody] UserUpdateDto userUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<UsersDto>($"Invalid data: {errors}", 400);
                }

                var user = await _userManager.FindByIdAsync(id);
                
                if (user == null)
                {
                    return Error<UsersDto>($"User with ID {id} not found", 404);
                }

                // Update user properties
                if (!string.IsNullOrEmpty(userUpdateDto.UserName))
                    user.UserName = userUpdateDto.UserName;
                    
                if (!string.IsNullOrEmpty(userUpdateDto.FirstName))
                    user.FirstName = userUpdateDto.FirstName;
                    
                if (!string.IsNullOrEmpty(userUpdateDto.LastName))
                    user.LastName = userUpdateDto.LastName;
                    
                if (!string.IsNullOrEmpty(userUpdateDto.Email))
                    user.Email = userUpdateDto.Email;
                    
                if (!string.IsNullOrEmpty(userUpdateDto.PhoneNumber))
                    user.PhoneNumber = userUpdateDto.PhoneNumber;
                    
                if (userUpdateDto.DOB != default(DateTime))
                    user.DOB = userUpdateDto.DOB;
                    
                user.Active = userUpdateDto.Active;
                    
                user.Updated = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                
                if (!result.Succeeded)
                {
                    return Error<UsersDto>($"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}", 400);
                }

                // Return updated user with roles
                var updatedUser = new UsersDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumber = user.PhoneNumber,
                    DOB = user.DOB,
                    Created = user.Created,
                    Updated = user.Updated,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                    Active = user.Active
                };

                return Success(updatedUser, "User updated successfully");
            }
            catch (Exception ex)
            {
                return Error<UsersDto>($"Failed to update user: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Delete user (Admin/HallManager users only, not customers)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("users/{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                
                if (user == null)
                {
                    return Error($"User with ID {id} not found", 404);
                }

                // Prevent self-deletion
                if (user.Id == UserId)
                {
                    return Error("You cannot delete your own account", 400);
                }

                // Check if user is a customer - redirect to CustomerController
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Customer"))
                {
                    return Error("Cannot delete customer users through this endpoint. Use /api/customers/{id} instead.", 400);
                }

                var result = await _userManager.DeleteAsync(user);
                
                if (!result.Succeeded)
                {
                    return Error($"Failed to delete user: {string.Join(", ", result.Errors.Select(e => e.Description))}", 400);
                }

                return Success("User deleted successfully");
            }
            catch (Exception ex)
            {
                return Error($"Failed to delete user: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get orders for moderation
        /// </summary>
        /// <returns>Orders requiring moderation</returns>
        [HttpGet("orders/moderate")]
        public ActionResult<ApiResponse<string>> GetOrdersForModeration()
        {
            return Success("Only Admins/Moderators can access this", "Access granted to moderation panel");
        }

        /// <summary>
        /// Create new hall manager
        /// </summary>
        /// <param name="userCreateDto">Hall manager creation data</param>
        /// <returns>Created hall manager</returns>
        [HttpPost("hall-managers")]
        public async Task<ActionResult<ApiResponse<HallManagerProfileDto>>> CreateHallManager([FromBody] UserCreateDto userCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<HallManagerProfileDto>($"Invalid data: {errors}", 400);
                }

                // Create AppUser first
                var userEntity = _mapper.Map<HallApp.Core.Entities.AppUser>(userCreateDto);
                var user = await _userService.CreateUserAsync(userEntity);

                if (user != null)
                {
                    // Create HallManager entity - just links AppUser to managed Halls
                    var hallManager = new HallManager
                    {
                        AppUserId = user.Id
                    };

                    var createdHallManager = await _hallManagerService.CreateHallManagerAsync(hallManager);
                    
                    // Get combined profile data
                    var profile = await _hallManagerProfileService.GetHallManagerProfileAsync(user.Id.ToString());
                    if (profile.HasValue)
                    {
                        var profileDto = _mapper.Map<HallManagerProfileDto>(profile.Value);
                        return Success(profileDto, "Hall manager created successfully");
                    }
                }

                return Error<HallManagerProfileDto>("Couldn't create hall manager", 500);
            }
            catch (Exception ex)
            {
                return Error<HallManagerProfileDto>($"Failed to create hall manager: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update hall manager
        /// </summary>
        /// <param name="userId">Hall manager user ID</param>
        /// <param name="userUpdateDto">Updated hall manager data</param>
        /// <returns>Updated hall manager</returns>
        [HttpPut("hall-managers/{userId}")]
        public async Task<ActionResult<ApiResponse<HallManagerProfileDto>>> UpdateHallManager(string userId, [FromBody] UserUpdateDto userUpdateDto)
        {
            try
            {
                // Get the profile and update it
                var profile = await _hallManagerProfileService.GetHallManagerProfileAsync(userId);
                if (profile.HasValue)
                {
                    // TODO: Implement update logic
                    var profileDto = _mapper.Map<HallManagerProfileDto>(profile.Value);
                    return Success(profileDto, "Hall manager retrieved successfully");
                }

                return Error<HallManagerProfileDto>("Hall manager not found", 404);
            }
            catch (Exception ex)
            {
                return Error<HallManagerProfileDto>($"Failed to update hall manager: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Delete hall manager
        /// </summary>
        /// <param name="userId">Hall manager user ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("hall-managers/{userId}")]
        public async Task<ActionResult<ApiResponse>> DeleteHallManager(string userId)
        {
            try
            {
                var result = await _hallManagerProfileService.DeleteHallManagerProfileAsync(userId);
                
                if (result)
                {
                    return Success("Hall manager deleted successfully");
                }

                return Error("Couldn't delete hall manager", 500);
            }
            catch (Exception ex)
            {
                return Error($"Failed to delete hall manager: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get all hall managers
        /// </summary>
        /// <returns>List of hall managers</returns>
        [HttpGet("hall-managers")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallManagerBusinessDto>>>> GetHallManagers()
        {
            try
            {
                var hallManagers = await _hallManagerService.GetAllHallManagersAsync();
                var hallManagerDtos = _mapper.Map<IEnumerable<HallManagerBusinessDto>>(hallManagers);
                return Success(hallManagerDtos, "Hall managers retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<HallManagerBusinessDto>>($"Failed to retrieve hall managers: {ex.Message}", 500);
            }
        }

        // NOTE: Hall CRUD operations moved to HallController (/api/halls)
        // Use /api/halls for all hall operations (GET, POST, PUT, DELETE)
        // Admin can still access halls via HallController with Admin role

        /// <summary>
        /// Get system statistics dashboard
        /// </summary>
        /// <returns>System statistics</returns>
        [HttpGet("dashboard/statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetDashboardStatistics()
        {
            try
            {
                // Get hall and booking counts using services
                var allHalls = await _hallService.GetAllHallsAsync();
                var allVendors = await _vendorService.GetVendorsAsync(null);
                
                var statistics = new
                {
                    TotalUsers = await _userManager.Users.CountAsync(),
                    TotalCustomers = await _userManager.Users.Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Customer")).CountAsync(),
                    TotalVendors = await _userManager.Users.Where(u => u.UserRoles.Any(ur => ur.Role.Name == "VendorManager")).CountAsync(),
                    TotalHallManagers = await _userManager.Users.Where(u => u.UserRoles.Any(ur => ur.Role.Name == "HallManager")).CountAsync(),
                    TotalHalls = allHalls?.Count ?? 0,
                    TotalVendorCount = allVendors?.Count() ?? 0,
                    TotalBookings = 0, // TODO: Implement booking count when BookingService is available
                    PendingBookings = 0, // TODO: Implement pending booking count
                    RecentRegistrations = await _userManager.Users.Where(u => u.Created >= DateTime.UtcNow.AddDays(-7)).CountAsync()
                };

                return Success<object>(statistics, "Dashboard statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to retrieve dashboard statistics: {ex.Message}", 500);
            }
        }
    }
}
