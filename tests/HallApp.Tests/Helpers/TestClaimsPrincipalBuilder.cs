using System.Security.Claims;

namespace HallApp.Tests.Helpers;

/// <summary>
/// Helper to build ClaimsPrincipal for controller-level tests.
/// Simulates JWT-authenticated users with various roles.
/// </summary>
public static class TestClaimsPrincipalBuilder
{
    /// <summary>
    /// Creates a ClaimsPrincipal representing a HallManager user.
    /// </summary>
    public static ClaimsPrincipal CreateHallManager(int userId, string? userName = null)
    {
        return CreateUser(userId, userName ?? $"hallmanager{userId}@test.com", "HallManager");
    }

    /// <summary>
    /// Creates a ClaimsPrincipal representing a Customer user.
    /// </summary>
    public static ClaimsPrincipal CreateCustomer(int userId, string? userName = null)
    {
        return CreateUser(userId, userName ?? $"customer{userId}@test.com", "Customer");
    }

    /// <summary>
    /// Creates a ClaimsPrincipal representing an Admin user.
    /// </summary>
    public static ClaimsPrincipal CreateAdmin(int userId = 999, string? userName = null)
    {
        return CreateUser(userId, userName ?? "admin@test.com", "Admin");
    }

    /// <summary>
    /// Creates a ClaimsPrincipal representing a VendorManager user.
    /// </summary>
    public static ClaimsPrincipal CreateVendorManager(int userId, string? userName = null)
    {
        return CreateUser(userId, userName ?? $"vendormanager{userId}@test.com", "VendorManager");
    }

    /// <summary>
    /// Creates an unauthenticated ClaimsPrincipal.
    /// </summary>
    public static ClaimsPrincipal CreateAnonymous()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    private static ClaimsPrincipal CreateUser(int userId, string userName, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Email, userName),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
