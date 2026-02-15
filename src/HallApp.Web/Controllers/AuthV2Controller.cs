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
using HallApp.Web.Services;

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
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<AuthV2Controller> _logger;

    public AuthV2Controller(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        DataContext context,
        IUserRepository userRepository,
        IFileUploadService fileUploadService,
        ILogger<AuthV2Controller> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _context = context;
        _userRepository = userRepository;
        _fileUploadService = fileUploadService;
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

    /// <summary>
    /// Refresh access token using a valid refresh token.
    /// Implements token rotation: old refresh token is invalidated, new one is issued.
    /// </summary>
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
                _logger.LogWarning("Invalid refresh token format or signature");
                return Unauthorized(ErrorResponse("Invalid refresh token"));
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.NameId)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Refresh token missing user ID claim");
                return Unauthorized(ErrorResponse("Invalid refresh token"));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Refresh token for non-existent user {UserId}", userId);
                return Unauthorized(ErrorResponse("Invalid refresh token"));
            }

            // SECURITY: Token Rotation - verify the refresh token matches stored token
            if (user.RefreshToken != dto.RefreshToken)
            {
                // Potential token reuse attack! Invalidate all tokens for this user
                _logger.LogWarning(
                    "Refresh token mismatch for user {UserId} - possible replay attack. Invalidating all tokens.",
                    userId);

                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1);
                await _userManager.UpdateAsync(user);

                return Unauthorized(ErrorResponse("Invalid refresh token. Please log in again."));
            }

            // Check if refresh token has expired
            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Expired refresh token used for user {UserId}", userId);
                return Unauthorized(ErrorResponse("Refresh token has expired. Please log in again."));
            }

            // Token rotation: Generate new tokens
            var response = await BuildAuthResponse(user);

            _logger.LogInformation("Token refreshed for user {UserId} (v2)", userId);
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
            var userId = GetCurrentUserId();
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

    /// <summary>
    /// Get the currently authenticated user's profile.
    /// Accessible via both GET /api/v2/auth/me and GET /api/v2/auth/profile.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [HttpGet("profile")]
    public async Task<ActionResult<UserV2Dto>> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ErrorResponse("User not authenticated"));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(ErrorResponse("User not found"));

            var userDto = MapToUserDto(user);
            userDto.Roles = await _userManager.GetRolesAsync(user);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user (v2). UserId claim: {UserId}. Exception: {ExMessage}. Inner: {InnerMessage}",
                GetCurrentUserId() ?? "null",
                ex.Message,
                ex.InnerException?.Message ?? "none");
            return StatusCode(500, ErrorResponse("An unexpected error occurred. Please try again."));
        }
    }

    // ── Update Profile (Self) ────────────────────────────────────────────

    /// <summary>
    /// Update the authenticated user's own profile. UserId is extracted from JWT - not from request.
    /// </summary>
    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserV2Dto>> UpdateProfile([FromBody] UpdateProfileV2Dto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ErrorResponse(GetFirstValidationError()));

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ErrorResponse("User not authenticated"));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(ErrorResponse("User not found"));

            // Update only provided fields (partial update)
            if (!string.IsNullOrEmpty(dto.FirstName))
                user.FirstName = dto.FirstName;

            if (!string.IsNullOrEmpty(dto.LastName))
                user.LastName = dto.LastName;

            if (!string.IsNullOrEmpty(dto.PhoneNumber))
                user.PhoneNumber = dto.PhoneNumber;

            if (!string.IsNullOrEmpty(dto.Gender))
                user.Gender = dto.Gender;

            if (dto.DateOfBirth.HasValue)
            {
                // Npgsql requires DateTimeKind.Utc for 'timestamp with time zone' columns.
                // The frontend may send date-only strings (e.g. "1990-05-15") which deserialize
                // as DateTimeKind.Unspecified, causing a PostgreSQL exception on save.
                var dob = dto.DateOfBirth.Value;
                user.DOB = dob.Kind == DateTimeKind.Utc
                    ? dob
                    : DateTime.SpecifyKind(dob, DateTimeKind.Utc);
            }

            user.Updated = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Profile update failed";
                _logger.LogWarning("Profile update failed for user {UserId}: {Error}", userId, error);
                return BadRequest(ErrorResponse(error));
            }

            _logger.LogInformation("User {UserId} updated their profile (v2)", userId);

            var userDto = MapToUserDto(user);
            userDto.Roles = await _userManager.GetRolesAsync(user);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile (v2). UserId: {UserId}. Exception: {ExMessage}. Inner: {InnerMessage}",
                GetCurrentUserId() ?? "null",
                ex.Message,
                ex.InnerException?.Message ?? "none");
            return StatusCode(500, ErrorResponse("An unexpected error occurred. Please try again."));
        }
    }

    // ── Upload Profile Photo ─────────────────────────────────────────────

    /// <summary>
    /// Upload a profile photo for the authenticated user. All roles are permitted.
    /// Accepts multipart/form-data with a single file field named "photo".
    /// Max 5 MB, allowed types: jpg, jpeg, png, gif, webp.
    /// </summary>
    [Authorize]
    [HttpPost("upload-photo")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UserV2Dto>> UploadProfilePhoto([FromForm] IFormFile photo)
    {
        try
        {
            if (photo == null || photo.Length == 0)
                return BadRequest(ErrorResponse("No photo file provided"));

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ErrorResponse("User not authenticated"));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(ErrorResponse("User not found"));

            // Delete old photo if it exists
            if (!string.IsNullOrEmpty(user.PhotoUrl))
            {
                try
                {
                    await _fileUploadService.DeleteImageAsync(user.PhotoUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old profile photo for user {UserId}", userId);
                }
            }

            // Save new photo to "avatars" folder
            var photoUrl = await _fileUploadService.SaveImageAsync(photo, "avatars");

            // Update user record
            user.PhotoUrl = photoUrl;
            user.Updated = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Failed to update profile photo";
                _logger.LogWarning("Photo upload failed for user {UserId}: {Error}", userId, error);
                return BadRequest(ErrorResponse(error));
            }

            _logger.LogInformation("User {UserId} uploaded a profile photo (v2)", userId);

            var userDto = MapToUserDto(user);
            userDto.Roles = await _userManager.GetRolesAsync(user);
            return Ok(userDto);
        }
        catch (ArgumentException ex)
        {
            // File validation errors from FileUploadService (size, type)
            return BadRequest(ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile photo (v2)");
            return StatusCode(500, ErrorResponse("An unexpected error occurred. Please try again."));
        }
    }

    // ── Change Password ──────────────────────────────────────────────────

    /// <summary>
    /// Change password for the authenticated user. Requires current password verification.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordV2Dto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ErrorResponse(GetFirstValidationError()));

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ErrorResponse("User not authenticated"));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(ErrorResponse("User not found"));

            // Verify current password
            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, dto.CurrentPassword, false);
            if (!passwordCheck.Succeeded)
            {
                _logger.LogWarning("Password change failed for user {UserId}: incorrect current password", userId);
                return BadRequest(ErrorResponse("Current password is incorrect"));
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Password change failed";
                _logger.LogWarning("Password change failed for user {UserId}: {Error}", userId, error);
                return BadRequest(ErrorResponse(error));
            }

            // Invalidate refresh token to force re-login on other devices
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1);
            await _userManager.UpdateAsync(user);

            // Update security stamp to invalidate existing tokens
            await _userManager.UpdateSecurityStampAsync(user);

            _logger.LogInformation("User {UserId} changed their password (v2)", userId);
            return Ok(new { message = "Password changed successfully. Please log in again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password (v2)");
            return StatusCode(500, ErrorResponse("An unexpected error occurred. Please try again."));
        }
    }

    // ── Forgot Password ──────────────────────────────────────────────────

    /// <summary>
    /// Request a password reset. Sends reset token to user's email.
    /// Returns success even if email doesn't exist (security: prevent email enumeration).
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ResetPasswordRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ErrorResponse(GetFirstValidationError()));

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist - security best practice
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", dto.Email);
                return Ok(new { message = "If this email exists, a password reset link has been sent." });
            }

            // Generate password reset token
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Log that reset was requested (without sensitive token data)
            _logger.LogInformation("Password reset email sent to {Email}", dto.Email);

            // TODO: Implement email sending service
            // await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);

            return Ok(new { message = "If this email exists, a password reset link has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password (v2)");
            return StatusCode(500, ErrorResponse("An unexpected error occurred. Please try again."));
        }
    }

    // ── Reset Password ───────────────────────────────────────────────────

    /// <summary>
    /// Reset password using the token received via email.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ErrorResponse(GetFirstValidationError()));

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                return BadRequest(ErrorResponse("Invalid or expired reset token"));
            }

            // Validate and reset password
            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Password reset failed";
                _logger.LogWarning("Password reset failed for user {UserId}: {Error}", user.Id, error);
                return BadRequest(ErrorResponse(error));
            }

            // Invalidate all refresh tokens
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1);
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} reset their password (v2)", user.Id);
            return Ok(new { message = "Password has been reset successfully. Please log in with your new password." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset (v2)");
            return StatusCode(500, ErrorResponse("An unexpected error occurred. Please try again."));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Extract user ID from JWT claims. Checks multiple claim types for compatibility.
    /// </summary>
    private string? GetCurrentUserId()
    {
        // Check custom "id" claim first (added by TokenService)
        var userId = User.FindFirstValue("id");
        if (!string.IsNullOrEmpty(userId))
            return userId;

        // Fallback to standard claims
        userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
            return userId;

        // Fallback to JWT registered claims
        userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!string.IsNullOrEmpty(userId))
            return userId;

        userId = User.FindFirstValue(JwtRegisteredClaimNames.NameId);
        return userId;
    }

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

        var userDto = MapToUserDto(user);
        userDto.Roles = await _userManager.GetRolesAsync(user);

        return new AuthV2ResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = userDto
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
        Created = user.Created,
        DateOfBirth = user.DOB,
        PhotoUrl = user.PhotoUrl
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
