using Bogus;
using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data.Seeding.Fakers;

/// <summary>
/// Bogus faker for ApplicationUser entity.
/// Generates realistic customer accounts for demo purposes.
/// </summary>
public sealed class CustomerFaker : Faker<ApplicationUser>
{
    public CustomerFaker()
    {
        // Locale for Vietnamese names (fallback to English if not available)
        UseSeed(42); // Deterministic for reproducibility

        RuleFor(u => u.Email, f => f.Internet.Email(provider: "healthsync.demo").ToLowerInvariant())
            .RuleFor(u => u.Role, "Customer")
            .RuleFor(u => u.IsActive, true)
            .RuleFor(u => u.OauthProvider, f => f.Random.Bool(0.2f) ? f.PickRandom("Google", "Facebook") : null)
            .RuleFor(u => u.OauthProviderId, (f, u) => u.OauthProvider != null ? f.Random.AlphaNumeric(21) : null)
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(1, DateTime.UtcNow.AddDays(-30)))
            .RuleFor(u => u.LastLoginAt, (f, u) => f.Date.Between(u.CreatedAt, DateTime.UtcNow));
    }

    /// <summary>
    /// Creates a new faker with Vietnamese locale.
    /// </summary>
    public static CustomerFaker Create() => new();
}
