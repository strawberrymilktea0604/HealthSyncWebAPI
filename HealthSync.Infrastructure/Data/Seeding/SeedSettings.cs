namespace HealthSync.Infrastructure.Data.Seeding;

/// <summary>
/// Configuration settings for data seeding.
/// Mapped from appsettings.json "SeedSettings" section.
/// </summary>
public class SeedSettings
{
    public const string SectionName = "SeedSettings";

    /// <summary>
    /// Enable/disable data seeding entirely.
    /// Default: false (safe for production)
    /// </summary>
    public bool EnableDataSeeding { get; set; } = false;

    /// <summary>
    /// Enable/disable demo data seeding (Users, Logs, Posts).
    /// Only used when EnableDataSeeding is true.
    /// Default: false (only catalog data)
    /// </summary>
    public bool SeedDemoData { get; set; } = false;

    /// <summary>
    /// Number of demo customers to create.
    /// Default: 20
    /// </summary>
    public int DemoCustomerCount { get; set; } = 20;

    /// <summary>
    /// Number of days of activity logs to generate per user.
    /// Default: 30
    /// </summary>
    public int ActivityLogDays { get; set; } = 30;

    /// <summary>
    /// Path to seed images folder inside container.
    /// Default: /app/seed-data/images
    /// </summary>
    public string SeedImagePath { get; set; } = "/app/seed-data/images";

    /// <summary>
    /// Default password for demo customers.
    /// Should be changed in production.
    /// </summary>
    public string DefaultCustomerPassword { get; set; } = "Demo@123456";
}
