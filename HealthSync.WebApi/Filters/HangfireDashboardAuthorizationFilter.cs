using Hangfire.Dashboard;
using System.Security.Claims;

namespace HealthSync.WebApi.Filters;

/// <summary>
/// Authorization filter for Hangfire Dashboard
/// Allows access in development mode, requires Admin role in production
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return AuthorizeUser(httpContext.User);
    }

    // Extracted for testing
    public bool AuthorizeUser(ClaimsPrincipal user)
    {
        // Check if user is authenticated
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Check if user has Admin role
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        return roleClaim == "Admin";
    }
}
