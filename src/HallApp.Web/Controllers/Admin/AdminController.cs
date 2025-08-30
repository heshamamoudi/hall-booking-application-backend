using HallApp.Web.Controllers.Common;
using HallApp.Application.Common.Models;
using HallApp.Application.DTOs.Admin;
using HallApp.Application.DTOs.Champer.Hall;
using HallApp.Application.DTOs.Champer.HallManager;
using HallApp.Application.DTOs.HallManager;
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
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/v1/admin")]
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUserService _userService;
        private readonly IHallService _hallService;
        private readonly IVendorService _vendorService;
        private readonly IHallManagerService _hallManagerService;
        private readonly IHallManagerProfileService _hallManagerProfileService;
        private readonly IVendorManagerService _vendorManagerService;
        private readonly IVendorProfileService _vendorProfileService;
        private readonly IMapper _mapper;

        public AdminController(
            IUserService userService,
            IHallService hallService,
            IVendorService vendorService,
            IHallManagerService hallManagerService,
            IHallManagerProfileService hallManagerProfileService,
            IVendorManagerService vendorManagerService,
            IVendorProfileService vendorProfileService,
            IMapper mapper,
            UserManager<HallApp.Core.Entities.AppUser> userManager)
        {
            _userService = userService;
            _hallService = hallService;
            _vendorService = vendorService;
            _hallManagerService = hallManagerService;
            _hallManagerProfileService = hallManagerProfileService;
            _vendorManagerService = vendorManagerService;
            _vendorProfileService = vendorProfileService;
            _mapper = mapper;
            _userManager = userManager;
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
                    // Create HallManager business entity with default values
                    var hallManager = new HallManager
                    {
                        AppUserId = user.Id,
                        CompanyName = "Default Company",
                        CommercialRegistrationNumber = $"REG-{user.Id}-{DateTime.Now.Ticks}",
                        IsApproved = false
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
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<HallManagerProfileDto>($"Invalid data: {errors}", 400);
                }

                // Map user and hall manager data
                var userData = _mapper.Map<HallApp.Core.Entities.AppUser>(userUpdateDto);
                var hallManagerData = new HallManager
                {
                    CompanyName = "Updated Company",
                    CommercialRegistrationNumber = $"REG-UPD-{DateTime.Now.Ticks}"
                };

                var result = await _hallManagerProfileService.UpdateHallManagerProfileAsync(userId, userData, hallManagerData);
                
                if (result)
                {
                    var profile = await _hallManagerProfileService.GetHallManagerProfileAsync(userId);
                    if (profile.HasValue)
                    {
                        var profileDto = _mapper.Map<HallManagerProfileDto>(profile.Value);
                        return Success(profileDto, "Hall manager updated successfully");
                    }
                }

                return Error<HallManagerProfileDto>("Couldn't update hall manager", 500);
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

        /// <summary>
        /// Get all halls (Admin gets all, HallManager gets their own)
        /// </summary>
        /// <returns>List of halls</returns>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpGet("halls")]
        public async Task<ActionResult<ApiResponse<IEnumerable<HallDto>>>> GetHalls()
        {
            try
            {
                List<HallDto> halls = new List<HallDto>();

                if (IsAdmin)
                {
                    var hallEntities = await _hallService.GetAllHallsAsync();
                    if (hallEntities == null || !hallEntities.Any())
                    {
                        return Error<IEnumerable<HallDto>>("No halls found", 404);
                    }
                    halls = _mapper.Map<List<HallDto>>(hallEntities);
                }
                else if (IsHallManager)
                {
                    var hallEntities = await _hallService.GetHallsByManagerAsync(UserId.ToString());
                    if (hallEntities == null || !hallEntities.Any())
                    {
                        return Error<IEnumerable<HallDto>>("No halls found for this manager", 404);
                    }
                    halls = _mapper.Map<List<HallDto>>(hallEntities);
                }
                else
                {
                    return Error<IEnumerable<HallDto>>("Access denied", 403);
                }

                return Success<IEnumerable<HallDto>>(halls, "Halls retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<HallDto>>($"Failed to retrieve halls: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Create new hall
        /// </summary>
        /// <param name="hallCreateDto">Hall creation data</param>
        /// <returns>Created hall</returns>
        [HttpPost("halls")]
        public async Task<ActionResult<ApiResponse<HallDto>>> CreateHall([FromBody] HallCreateDto hallCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<HallDto>($"Invalid data: {errors}", 400);
                }

                var hallEntity = _mapper.Map<Hall>(hallCreateDto);
                var createdHall = await _hallService.CreateHallAsync(hallEntity);
                var hallDto = _mapper.Map<HallDto>(createdHall);

                if (createdHall != null)
                {
                    return Success(hallDto, "Hall created successfully");
                }
                return Error<HallDto>("Couldn't create hall", 500);
            }
            catch (Exception ex)
            {
                return Error<HallDto>($"Failed to create hall: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update hall
        /// </summary>
        /// <param name="hallUpdateDto">Updated hall data</param>
        /// <returns>Updated hall</returns>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpPut("halls")]
        public async Task<ActionResult<ApiResponse<HallDto>>> UpdateHall([FromBody] HallUpdateDto hallUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<HallDto>($"Invalid data: {errors}", 400);
                }

                Hall updatedHallEntity;
                if (IsAdmin)
                {
                    var hallEntity = _mapper.Map<Hall>(hallUpdateDto);
                    updatedHallEntity = await _hallService.UpdateHallAsync(hallEntity);
                }
                else if (IsHallManager)
                {
                    var hallEntity = _mapper.Map<Hall>(hallUpdateDto);
                    updatedHallEntity = await _hallService.UpdateHallAsync(hallEntity);
                }
                else
                {
                    return Error<HallDto>("Access denied", 403);
                }

                if (updatedHallEntity == null)
                {
                    return Error<HallDto>("Couldn't update hall", 500);
                }
                
                var updatedHall = _mapper.Map<HallDto>(updatedHallEntity);
                if (updatedHall == null)
                {
                    return Error<HallDto>("Couldn't update hall", 500);
                }
                return Success(updatedHall, "Hall updated successfully");
            }
            catch (Exception ex)
            {
                return Error<HallDto>($"Failed to update hall: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get system statistics dashboard
        /// </summary>
        /// <returns>System statistics</returns>
        [HttpGet("dashboard/statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetDashboardStatistics()
        {
            try
            {
                var statistics = new
                {
                    TotalUsers = await _userManager.Users.CountAsync(),
                    TotalCustomers = await _userManager.Users.Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Customer")).CountAsync(),
                    TotalVendors = await _userManager.Users.Where(u => u.UserRoles.Any(ur => ur.Role.Name == "VendorManager")).CountAsync(),
                    TotalHallManagers = await _userManager.Users.Where(u => u.UserRoles.Any(ur => ur.Role.Name == "HallManager")).CountAsync(),
                    TotalHalls = 0, // await _unitOfWork.HallRepository.GetTotalHallsCountAsync(),
                    TotalBookings = 0, // await _unitOfWork.BookingRepository.GetTotalBookingsCountAsync(),
                    PendingBookings = 0, // await _unitOfWork.BookingRepository.GetPendingBookingsCountAsync(),
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
