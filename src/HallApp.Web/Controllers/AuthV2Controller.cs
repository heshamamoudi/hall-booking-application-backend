using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using HallApp.Core.Entities;
using HallApp.Application.DTOs.Auth;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Data;

namespace HallApp.Web.Controllers;

/// <summary>
/// V2 Auth endpoints with consistent error handling (Custom Crafts pattern).
/// All errors return { "message": "..." } for easy client-side parsing.
/// </summary>
[ApiController]
[Route("api/v2/auth")]
[Produces("application/json")]
public class AuthV2Controller : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly DataContext _context;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthV2Controller> _logger;

    public AuthV2Controller(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        DataContext context,
        IUserRepository userRepository,
        ILogger<AuthV2Controller> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _context = context;
        _userRepository = userRepository;
        _logger = logger;
    }

    // ── Check Email ───────────────────────────────────────────────────────

    [HttpPost("check-email")]
    public async Task<ActionResult<CheckEmailResponseDto>> CheckEmail([FromBody] CheckEmailRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ErrorResponse(GetFirstValidationError()));

        var user = await _userManager.FindByEmailAsync(dto.Email);
        return Ok(new CheckEmailResponseDto { Exists = user != null });
    }

    // ── Login ─────────────────────────────────────────────────────────────

    [HttpPost("login")]
    public async Task<ActionResult<AuthV2ResponseDto>> Login([FromBody] LoginV2Dto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ErrorResponse(GetFirstValidationError()));

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed — email not found: {Email}", dto.Email);
                return Unauthorized(ErrorResponse("Invalid email or password"));
            }

            if (!user.Active)
            {
                _logger.LogWarning("Login failed — account deactivated: {Email}", dto.Email);
                return Unauthorized(ErrorResponse("Your account has been deactivated. Please contact support."));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Login failed — account locked: {Email}", dto.Email);
                    return Unauthorized(ErrorResponse("Account is temporarily locked. Please try again later."));
                }
                _logger.LogWarning("Login failed — invalid password: {Email}", dto.Email);
                return Unauthorized(ErrorResponse("Invalid email or password"));
            }

            var response = await BuildAuthResponse(user);
            _logger.LogInformation("User {Email} logged in (v2)", user.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during v2 login for {Email}", dto.Email);
            return StatusCode(500, ErrorResponse("An unexpected error occurred. Please try again."));
        }
    }

    // ── Register ──────────────────────────────────────────────────────────

    [HttpPost("register")]
    public async Task<ActionResult<AuthV2ResponseDto>> Register([FromBody] RegisterV2Dto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ErrorResponse(GetFirstValidationError()));

            // Check duplicate email
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                // Check if this is an orphaned user (user exists but no Customer record)
                var hasCustomer = await _context.Customers.AnyAsync(c => c.AppUserId == existingUser.Id);
                if (!hasCustomer)
                {
                    // Orphaned user from a previous failed registration — delete and re-register
                    _logger.LogWarning("Cleaning up orphaned user {Email} (no Customer record)", dto.Email);
                    await _userManager.DeleteAsync(existingUser);
                }
                else
                {
                    return BadRequest(ErrorResponse("User with this email already exists"));
                }
            }

            // Generate username from email (part before @)
            var baseUserName = dto.Email.Split('@')[0];
            var userName = baseUserName;
            var counter = 1;
            while (await _userManager.FindByNameAsync(userName) != null)
            {
                userName = $"{baseUserName}{counter++}";
            }

            var user = new AppUser
            {
                UserName = userName,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                Gender = dto.Gender ?? "NotSpecified",
                DOB = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var firstError = createResult.Errors.FirstOrDefault()?.Description
                    ?? "Registration failed";
                _logger.LogWarning("Registration failed for {Email}: {Error}", dto.Email, firstError);
                return BadRequest(ErrorResponse(firstError));
            }

            await _userManager.AddToRoleAsync(user, "Customer");

            // Create Customer entity directly via DataContext (avoids UnitOfWork tracking conflicts)
            try
            {
                var customer = new HallApp.Core.Entities.CustomerEntities.Customer
                {
                    AppUserId = user.Id,
                    CreditMoney = 0,
                    Active = true,
                    Confirmed = false,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }
            catch (Exception custEx)
            {
                _logger.LogError(custEx, "Failed to create Customer entity for user {UserId}: {Msg}", user.Id, custEx.InnerException?.Message ?? custEx.Message);
            }

            // Generate tokens
            var accessToken = await _tokenService.CreateToken(user);
            var refreshToken = await _tokenService.CreateRefreshToken(user);

            var response = new AuthV2ResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = MapToUserDto(user)
            };

            _logger.LogInformation("Customer {Email} registered (v2)", user.Email);
            return CreatedAtAction(nameof(GetCurrentUser), response);
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? "no inner exception";
            _logger.LogError(ex, "Unexpected error during v2 registration for {Email}: {Message} | Inner: {Inner}", dto.Email, ex.Message, innerMsg);
            return StatusCode(500, ErrorResponse($"Registration error: {ex.Message} | {innerMsg}"));
        }
    }

    // ── Refresh Token ─────────────────────────────────────────────────────

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthV2ResponseDto>> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.RefreshToken))
                return BadRequest(ErrorResponse("Refresh token is required"));

            ClaimsPrincipal principal;
            try
            {
                principal = await _tokenService.ValidateRefreshToken(dto.RefreshToken);
            }
            catch
            {
                return Unauthorized(ErrorResponse("Invalid refresh token"));
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.NameId);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.RefreshToken != dto.RefreshToken)
                return Unauthorized(ErrorResponse("Invalid refresh token"));

            var response = await BuildAuthResponse(user);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during v2 token refresh");
            return StatusCode(500, ErrorResponse("An unexpected error occurred. Please try again."));
        }
    }

    // ── Logout ────────────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ErrorResponse("User not authenticated"));

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1);
                await _userManager.UpdateAsync(user);
            }

            _logger.LogInformation("User {UserId} logged out (v2)", userId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during v2 logout");
            return StatusCode(500, ErrorResponse("An unexpected error occurred. Please try again."));
        }
    }

    // ── Get Current User ──────────────────────────────────────────────────

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserV2Dto>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ErrorResponse("User not authenticated"));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(ErrorResponse("User not found"));

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user (v2)");
            return StatusCode(500, ErrorResponse("An unexpected error occurred. Please try again."));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<AuthV2ResponseDto> BuildAuthResponse(AppUser user)
    {
        var accessToken = await _tokenService.CreateToken(user);
        var refreshToken = await _tokenService.CreateRefreshToken(user);

        try
        {
            await _userRepository.RegisterOrUpdateTokenAsync(user, accessToken, refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist tokens for user {UserId}, continuing", user.Id);
        }

        return new AuthV2ResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = MapToUserDto(user)
        };
    }

    private static UserV2Dto MapToUserDto(AppUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        Gender = user.Gender,
        EmailConfirmed = user.EmailConfirmed,
        Created = user.Created
    };

    /// <summary>
    /// Consistent error shape: { "message": "..." }
    /// </summary>
    private static object ErrorResponse(string message) => new { message };

    private string GetFirstValidationError()
    {
        var firstError = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault();
        return firstError ?? "Invalid request";
    }
}
