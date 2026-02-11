using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Tests.Helpers;

/// <summary>
/// Helper to set up ControllerBase instances with mocked HTTP context and user claims.
/// </summary>
public static class ControllerTestSetup
{
    /// <summary>
    /// Assigns a ClaimsPrincipal to a controller's HttpContext so that
    /// User, User.IsInRole, UserId, and related properties work correctly in tests.
    /// </summary>
    public static void SetUser(ControllerBase controller, ClaimsPrincipal user)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user,
                Request = { Path = "/api/test" }
            }
        };
    }
}
