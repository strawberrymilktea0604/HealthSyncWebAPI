namespace HealthSync.Infrastructure.Data.Seeding;

/// <summary>
/// Interface for database seeding operations.
/// Supports idempotent seeding for Production and CI/CD environments.
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Seeds the database with initial data.
    /// Catalog data (Exercises, Foods, Categories) is always seeded.
    /// Demo data (Users, Logs, Posts) is seeded based on configuration.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
