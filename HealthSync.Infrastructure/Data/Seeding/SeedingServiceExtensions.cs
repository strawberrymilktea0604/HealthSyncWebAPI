using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthSync.Infrastructure.Data.Seeding;

/// <summary>
/// Extension methods for registering data seeding services.
/// </summary>
public static class SeedingServiceExtensions
{
    /// <summary>
    /// Adds data seeding services to the service collection.
    /// </summary>
    public static IServiceCollection AddDataSeeding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure SeedSettings from appsettings.json
        services.Configure<SeedSettings>(
            configuration.GetSection(SeedSettings.SectionName));

        // Register seeder
        services.AddScoped<IDataSeeder, DataSeeder>();

        return services;
    }
}
