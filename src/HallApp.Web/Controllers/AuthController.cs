using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using HallApp.Core.Entities;
using HallApp.Core.Exceptions;
using HallApp.Application.DTOs.Auth;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Web.Controllers.Common;

namespace HallApp.Web.Controllers
{
    [Route("api/auth")]
    public class AuthController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ICustomerService _customerService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ITokenService tokenService,
            ICustomerService customerService,
            IUserRepository userRepository,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _customerService = customerService;
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// Register new customer account
        /// </summary>
        /// <param name="registerDto">Customer registration data</param>
        /// <returns>Authentication response with tokens</returns>
        [HttpPost("register")]
        [HttpPost("register-customer")] // Alternative route for clarity
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] CustomerRegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse(400, "Invalid registration data", ModelState));
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email) ??
                                 await _userManager.FindByNameAsync(registerDto.UserName);

                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse(400, "User already exists"));
                }

                // Create new user based on role
                var user = new AppUser
                {
                    UserName = registerDto.UserName,
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    PhoneNumber = registerDto.PhoneNumber,
                    Gender = registerDto.Gender ?? "NotSpecified", // Provide default value if not specified
                    DOB = registerDto.DOB ?? new DateTime(1900, 1, 1),
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponse(400, "User creation failed", result.Errors));
                }

                // Always assign Customer role for public registration
                await _userManager.AddToRoleAsync(user, "Customer");

                // Create Customer entity linked to AppUser
                var customer = new HallApp.Core.Entities.CustomerEntities.Customer
                {
                    AppUserId = user.Id,
                    CreditMoney = 0,
                    Active = true,
                    Confirmed = false
                };

                await _customerService.CreateCustomerAsync(customer);

                // Generate tokens
                var accessToken = await _tokenService.CreateToken(user);
                var refreshToken = await _tokenService.CreateRefreshToken(user);

                var response = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1), // Match TokenService expiry
                    User = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber,
                        EmailConfirmed = user.EmailConfirmed,
                        Created = user.Created,
                        Updated = user.Updated
                    }
                };

                _logger.LogInformation("Customer {Email} registered successfully", user.Email);
                return CreatedAtAction(nameof(GetCurrentUser), response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new ApiResponse(500, "Registration failed"));
            }
        }

        /// <summary>
        /// Register new vendor account
        /// </summary>
        /// <param name="registerDto">Vendor registration data</param>
        /// <returns>Authentication response with tokens</returns>
        [HttpPost("register-vendor")]
        public async Task<ActionResult<AuthResponseDto>> RegisterVendor([FromBody] CustomerRegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse(400, "Invalid registration data", ModelState));
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email) ??
                                   await _userManager.FindByNameAsync(registerDto.UserName);

                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse(400, "User with this email or username already exists"));
                }

                // Create new vendor user
                var user = new AppUser
                {
                    UserName = registerDto.UserName,
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    PhoneNumber = registerDto.PhoneNumber,
                    Gender = registerDto.Gender ?? "NotSpecified",
                    DOB = registerDto.DOB ?? new DateTime(1900, 1, 1),
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponse(400, "User creation failed", result.Errors));
                }

                // Assign VendorManager role for vendor registration
                await _userManager.AddToRoleAsync(user, "VendorManager");

                // Generate tokens
                var accessToken = await _tokenService.CreateToken(user);
                var refreshToken = await _tokenService.CreateRefreshToken(user);

                // Store tokens in database for tracking
                await _userRepository.RegisterOrUpdateTokenAsync(user, accessToken, refreshToken);

                var response = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1), // Match TokenService expiry
                    User = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber,
                        EmailConfirmed = user.EmailConfirmed,
                        Created = user.Created,
                        Updated = user.Updated
                    }
                };

                _logger.LogInformation("Vendor {Email} registered successfully", user.Email);
                return CreatedAtAction(nameof(GetCurrentUser), response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during vendor registration");
                return StatusCode(500, new ApiResponse(500, "Vendor registration failed"));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse(400, "Invalid login data"));
                }

                var user = await _userManager.FindByEmailAsync(loginDto.Login) ??
                          await _userManager.FindByNameAsync(loginDto.Login);

                if (user == null)
                {
                    return Unauthorized(new ApiResponse(401, "Invalid credentials"));
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

                if (!result.Succeeded)
                {
                    return Unauthorized(new ApiResponse(401, "Invalid credentials"));
                }

                // Generate tokens
                var accessToken = await _tokenService.CreateToken(user);
                var refreshToken = await _tokenService.CreateRefreshToken(user);

                // Store tokens in database for tracking
                await _userRepository.RegisterOrUpdateTokenAsync(user, accessToken, refreshToken);

                var response = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1), // Match TokenService expiry
                    User = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber,
                        EmailConfirmed = user.EmailConfirmed,
                        Created = user.Created,
                        Updated = user.Updated
                    }
                };

                _logger.LogInformation("User {Email} logged in successfully", user.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new ApiResponse(500, "Login failed"));
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken(RefreshTokenDto refreshTokenDto)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshTokenDto.RefreshToken))
                {
                    return BadRequest(new ApiResponse(400, "Invalid refresh token"));
                }

                ClaimsPrincipal principal;
                try 
                {
                    principal = await _tokenService.ValidateRefreshToken(refreshTokenDto.RefreshToken);
                }
                catch
                {
                    return Unauthorized(new ApiResponse(401, "Invalid refresh token"));
                }

                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(JwtRegisteredClaimNames.NameId);
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null || user.RefreshToken != refreshTokenDto.RefreshToken)
                {
                    return Unauthorized(new ApiResponse(401, "Invalid refresh token"));
                }

                // Generate new tokens
                var newAccessToken = await _tokenService.CreateToken(user);
                var newRefreshToken = await _tokenService.CreateRefreshToken(user);

                var response = new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1), // Match TokenService expiry
                    User = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber,
                        EmailConfirmed = user.EmailConfirmed,
                        Created = user.Created,
                        Updated = user.Updated
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new ApiResponse(500, "Token refresh failed"));
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirstValue("id");
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse(401, "User not authenticated"));
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.RefreshToken = null;
                    var georgianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgia Standard Time");
                    user.RefreshTokenExpiryTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddDays(-1), georgianTimeZone);
                    await _userManager.UpdateAsync(user);
                }

                _logger.LogInformation("User {UserId} logged out successfully", userId);
                return Ok(new ApiResponse(200, "Logged out successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new ApiResponse(500, "Logout failed"));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("create-user")]
        public async Task<ActionResult<AuthResponseDto>> CreateUser(CreateUserDto createUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse(400, "Invalid user data", ModelState));
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email) ??
                                 await _userManager.FindByNameAsync(createUserDto.UserName);

                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse(400, "User already exists"));
                }

                // Create new user
                var user = new AppUser
                {
                    UserName = createUserDto.UserName,
                    Email = createUserDto.Email,
                    FirstName = createUserDto.FirstName,
                    LastName = createUserDto.LastName,
                    PhoneNumber = createUserDto.PhoneNumber,
                    Gender = createUserDto.Gender,
                    DOB = createUserDto.DOB ?? new DateTime(1900, 1, 1),
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, createUserDto.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponse(400, "User creation failed", result.Errors));
                }

                // Add the specified role (Admin or HallManager only)
                await _userManager.AddToRoleAsync(user, createUserDto.Role);

                // Generate tokens
                var accessToken = await _tokenService.CreateToken(user);
                var refreshToken = await _tokenService.CreateRefreshToken(user);

                var response = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1), // Match TokenService expiry
                    User = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber,
                        EmailConfirmed = user.EmailConfirmed,
                        Created = user.Created,
                        Updated = user.Updated
                    }
                };

                _logger.LogInformation("User {Email} created successfully with role {Role} by admin", user.Email, createUserDto.Role);
                return CreatedAtAction(nameof(GetCurrentUser), response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin user creation");
                return StatusCode(500, new ApiResponse(500, "User creation failed"));
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirstValue("id");
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse(401, "User not authenticated"));
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse(404, "User not found"));
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    Created = user.Created,
                    Updated = user.Updated
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new ApiResponse(500, "Failed to get user"));
            }
        }
    }
}
