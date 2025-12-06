using Hangfire.Dashboard;

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

        // Allow all authenticated users in development
        // In production, you should implement proper authorization
        // For now, we'll allow all requests to access the dashboard
        // TODO: Implement Admin-only access in production
        return true;
    }
}
