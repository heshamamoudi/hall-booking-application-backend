#nullable disable
using HallApp.Core.Entities;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Core.Interfaces.IServices;
using HallApp.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HallApp.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DataContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;

    public UserRepository(DataContext context, UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager, ITokenService tokenService)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    public async Task<IEnumerable<AppUser>> GetUsersAsync()
        => await _context.Users.ToListAsync();

    public async Task<AppUser> GetUserByPhoneAsync(string phoneNumber)
        => await _context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

    public async Task<AppUser> FindByUsernameAsync(string username)
        => await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

    public async Task<AppUser> FindByEmailAsync(string email)
        => await _userManager.Users.SingleOrDefaultAsync(x => x.Email == email);

    public async Task<SignInResult> CheckPasswordSignInAsync(AppUser user, string password)
        => await _signInManager.CheckPasswordSignInAsync(user, password, false);

    public async Task<string> GenerateTokenAsync(AppUser user)
        => await _tokenService.CreateToken(user);

    public async Task<string> GenerateRefreshTokenAsync(AppUser user)
        => await _tokenService.CreateRefreshToken(user);

    public async Task RegisterOrUpdateTokenAsync(AppUser user, string token, string refreshToken)
    {
        // Update the user's refresh token properties with Georgian timezone
        user.RefreshToken = refreshToken;
        var georgianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Georgia Standard Time");
        user.RefreshTokenExpiryTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddDays(7), georgianTimeZone);
        
        // Update the access token in the UserTokens table
        var existingToken = await _context.UserTokens
            .FirstOrDefaultAsync(ut => ut.UserId == user.Id
                && ut.LoginProvider == "HallAppApi"
                && ut.Name == "AuthToken");

        if (existingToken != null)
        {
            existingToken.Value = token;
            _context.UserTokens.Update(existingToken);
        }
        else
        {
            _context.UserTokens.Add(new IdentityUserToken<int>
            {
                UserId = user.Id,
                LoginProvider = "HallAppApi",
                Name = "AuthToken",
                Value = token
            });
        }

        // Save the refresh token in the UserTokens table
        var existingRefreshToken = await _context.UserTokens
            .FirstOrDefaultAsync(ut => ut.UserId == user.Id
                && ut.LoginProvider == "HallAppApi"
                && ut.Name == "RefreshToken");

        if (existingRefreshToken != null)
        {
            existingRefreshToken.Value = refreshToken;
            _context.UserTokens.Update(existingRefreshToken);
        }
        else
        {
            _context.UserTokens.Add(new IdentityUserToken<int>
            {
                UserId = user.Id,
                LoginProvider = "HallAppApi",
                Name = "RefreshToken",
                Value = refreshToken
            });
        }

        // Note: SaveChanges should be called by UnitOfWork.Complete()
        // await _context.SaveChangesAsync(); 
        await _userManager.UpdateAsync(user);
    }

    public async Task<ClaimsPrincipal> ValidateRefreshTokenAsync(string refreshToken)
        => await _tokenService.ValidateRefreshToken(refreshToken);

    public async Task<AppUser> FindByIdAsync(string userId)
        => await _userManager.FindByIdAsync(userId);

    public async Task<AppUser> GetUserByEmailAsync(string email)
        => await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

    public async Task<AppUser> GetUserByUsernameAsync(string username)
        => await _context.Users.FirstOrDefaultAsync(x => x.UserName == username);

    public async Task<AppUser> CreateCustomerUser(string userName, string phoneNumber, string email, 
        string firstName, string lastName, string gender, string password)
    {
        return await CreateUserWithRole(
            new AppUser
            {
                UserName = userName.ToLower(),
                PhoneNumber = phoneNumber,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Gender = gender
            },
            password,
            "Customer");
    }

    public async Task<AppUser> CreateUserWithRole(AppUser user, string password, string role)
    {
        var result = await _userManager.CreateAsync(user, password);
        
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, role);
        return user;
    }

    public async Task<bool> IsUsernameUniqueAsync(string username, int? excludeId = null)
    {
        var query = _context.Users.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId);
        }
        return !await query.AnyAsync(u => u.UserName.ToLower() == username.ToLower());
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null)
    {
        var query = _context.Users.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId);
        }
        return !await query.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeId = null)
    {
        if (string.IsNullOrEmpty(phone))
            return false;

        var query = _context.Users.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId);
        }
        return !await query.AnyAsync(u => u.PhoneNumber == phone);
    }

    public async Task<AppUser> GetUserById(int userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<AppUser> CreateUser(AppUser user)
    {
        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        return user;
    }

    public async Task<AppUser> UpdateUser(AppUser user)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        return user;
    }

    public async Task<bool> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        
        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<IEnumerable<AppUser>> GetHallManagersAsync()
    {
        return await _userManager.GetUsersInRoleAsync("HallManager");
    }

    public async Task<AppUser> AuthenticateUser(string login, string password, bool isVendorManager)
    {
        AppUser user = null;
        
        // Try to find user by email or username
        if (login.Contains("@"))
        {
            user = await FindByEmailAsync(login);
        }
        else
        {
            user = await FindByUsernameAsync(login);
        }

        if (user == null) return null;

        var result = await CheckPasswordSignInAsync(user, password);
        if (!result.Succeeded) return null;

        return user;
    }

    public async Task<bool> UserExists(string email, string phoneNumber)
    {
        return await _context.Users.AnyAsync(u => u.Email == email || u.PhoneNumber == phoneNumber);
    }

    public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new Exception("User not found");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task UpdateSecurityStampAsync(AppUser user)
    {
        await _userManager.UpdateSecurityStampAsync(user);
    }
}
