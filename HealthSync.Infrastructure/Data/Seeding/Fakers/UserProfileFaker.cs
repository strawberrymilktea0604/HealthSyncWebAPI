using Bogus;
using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data.Seeding.Fakers;

/// <summary>
/// Bogus faker for UserProfile entity.
/// Generates realistic user profile data for demo purposes.
/// </summary>
public sealed class UserProfileFaker : Faker<UserProfile>
{
    private static readonly string[] VietnameseFirstNames =
    {
        "Minh", "Anh", "Hương", "Linh", "Trang", "Hà", "Thảo", "Phương",
        "Dũng", "Tuấn", "Nam", "Hùng", "Quang", "Long", "Bình", "Đức",
        "Mai", "Lan", "Hoa", "Yến", "Ngọc", "Thùy", "Tâm", "Hiền"
    };

    private static readonly string[] VietnameseLastNames =
    {
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ",
        "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý"
    };

    public UserProfileFaker()
    {
        UseSeed(42);

        RuleFor(p => p.FullName, f => GenerateVietnameseName(f))
            .RuleFor(p => p.Gender, f => f.PickRandom<Gender>())
            .RuleFor(p => p.DateOfBirth, f => f.Date.Past(30, DateTime.Now.AddYears(-18)))
            .RuleFor(p => p.HeightCm, f => f.Random.Decimal(155, 185))
            .RuleFor(p => p.CurrentWeightKg, f => f.Random.Decimal(48, 95))
            .RuleFor(p => p.ActivityLevel, f => f.PickRandom<ActivityLevel>())
            .RuleFor(p => p.ContributionPoints, 0)
            .RuleFor(p => p.CreatedAt, f => DateTime.UtcNow)
            .RuleFor(p => p.UpdatedAt, f => DateTime.UtcNow);
    }

    private static string GenerateVietnameseName(Faker f)
    {
        var lastName = f.PickRandom(VietnameseLastNames);
        var middleName = f.Random.Bool(0.7f) ? f.PickRandom("Văn", "Thị", "Hoàng", "Minh", "Đức", "Ngọc") : "";
        var firstName = f.PickRandom(VietnameseFirstNames);

        return string.IsNullOrEmpty(middleName)
            ? $"{lastName} {firstName}"
            : $"{lastName} {middleName} {firstName}";
    }

    public static UserProfileFaker Create() => new();
}
