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
        var user = GetUser(context);
        return AuthorizeUser(user);
    }

    // Virtual for testing - allows test subclass to inject user without needing real DashboardContext
    protected virtual ClaimsPrincipal GetUser(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User;
    }

    // Extracted for testing
    public static bool AuthorizeUser(ClaimsPrincipal user)
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
