using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data.Seeding.Catalogs;

/// <summary>
/// Static catalog data for Community Challenges.
/// Provides idempotent seed data with prioritized Open and Upcoming challenges.
/// </summary>
public static class ChallengeCatalog
{
    /// <summary>
    /// Gets predefined challenges with appropriate statuses.
    /// Prioritizes Open (active) and Upcoming challenges.
    /// </summary>
    public static IReadOnlyList<ChallengeDefinition> GetChallenges(DateTime referenceDate)
    {
        return new List<ChallengeDefinition>
        {
            // ===== OPEN (Active) Challenges - Priority 1 =====
            new(
                Title: "7 Ngày Thay Đổi Lối Sống",
                Description: "Thử thách 7 ngày với chế độ ăn uống lành mạnh và tập luyện đều đặn. Hoàn thành ít nhất 5 ngày để chiến thắng!",
                ChallengeType: ChallengeType.Hybrid,
                DaysFromNow: -3,
                DurationDays: 10,
                Criteria: "Ghi lại nhật ký ăn uống và tập luyện ít nhất 5/7 ngày",
                Status: ChallengeStatus.Open,
                MaxParticipants: 100,
                RewardDescription: "Huy hiệu 'Người Tiên Phong' + 500 điểm",
                ImageFileName: "challenge_lifestyle.jpg"
            ),
            new(
                Title: "Cardio Champion",
                Description: "Thử thách cardio 21 ngày! Tích lũy tổng cộng 300 phút cardio trong 3 tuần.",
                ChallengeType: ChallengeType.Workout,
                DaysFromNow: -7,
                DurationDays: 21,
                Criteria: "Tổng thời gian cardio >= 300 phút trong 21 ngày",
                Status: ChallengeStatus.Open,
                MaxParticipants: 200,
                RewardDescription: "Huy hiệu 'Cardio Master' + 750 điểm + Voucher giảm giá",
                ImageFileName: "challenge_cardio.jpg"
            ),
            new(
                Title: "Eat Clean 14 Ngày",
                Description: "Thử thách ăn sạch trong 14 ngày. Tránh đồ ăn nhanh, đường tinh luyện và thực phẩm chế biến.",
                ChallengeType: ChallengeType.Nutrition,
                DaysFromNow: -5,
                DurationDays: 14,
                Criteria: "Ghi lại bữa ăn hàng ngày, không có thực phẩm chế biến sẵn",
                Status: ChallengeStatus.Open,
                MaxParticipants: 150,
                RewardDescription: "Huy hiệu 'Clean Eater' + 600 điểm",
                ImageFileName: "challenge_eatclean.jpg"
            ),
            new(
                Title: "Plank Challenge",
                Description: "Thử thách plank 30 ngày! Bắt đầu từ 30 giây và tăng dần lên 5 phút.",
                ChallengeType: ChallengeType.Workout,
                DaysFromNow: -10,
                DurationDays: 30,
                Criteria: "Hoàn thành plank hàng ngày theo lịch trình tăng dần",
                Status: ChallengeStatus.Open,
                MaxParticipants: 300,
                RewardDescription: "Huy hiệu 'Core King/Queen' + 1000 điểm",
                ImageFileName: "challenge_plank.jpg"
            ),

            // ===== UPCOMING Challenges - Priority 2 =====
            new(
                Title: "New Year Transformation",
                Description: "Thử thách biến đổi cơ thể 90 ngày đầu năm mới. Đặt mục tiêu và theo dõi tiến độ hàng tuần.",
                ChallengeType: ChallengeType.Hybrid,
                DaysFromNow: 7,
                DurationDays: 90,
                Criteria: "Giảm 5kg hoặc tăng 3kg cơ trong 90 ngày",
                Status: ChallengeStatus.Open,
                MaxParticipants: 500,
                RewardDescription: "Huy hiệu 'Transformation Hero' + 2000 điểm + Phần thưởng đặc biệt",
                ImageFileName: "challenge_transform.jpg"
            ),
            new(
                Title: "Morning Workout Streak",
                Description: "Thử thách tập luyện buổi sáng trong 21 ngày liên tục. Chỉ cần 15-30 phút mỗi sáng!",
                ChallengeType: ChallengeType.Workout,
                DaysFromNow: 14,
                DurationDays: 21,
                Criteria: "Tập luyện trước 9:00 sáng ít nhất 18/21 ngày",
                Status: ChallengeStatus.Open,
                MaxParticipants: 250,
                RewardDescription: "Huy hiệu 'Early Bird' + 800 điểm",
                ImageFileName: "challenge_morning.jpg"
            ),
            new(
                Title: "Hydration Hero",
                Description: "Thử thách uống đủ nước trong 30 ngày. Mục tiêu: 2-3 lít nước mỗi ngày.",
                ChallengeType: ChallengeType.Nutrition,
                DaysFromNow: 10,
                DurationDays: 30,
                Criteria: "Ghi lại lượng nước uống >= 2L/ngày trong 25/30 ngày",
                Status: ChallengeStatus.Open,
                MaxParticipants: 400,
                RewardDescription: "Huy hiệu 'Hydration Master' + 600 điểm",
                ImageFileName: "challenge_water.jpg"
            ),

            // ===== CLOSED (Completed) Challenges - For History =====
            new(
                Title: "Tháng 12 Fitness",
                Description: "Thử thách tập luyện cuối năm! Hoàn thành 20 buổi tập trong tháng 12.",
                ChallengeType: ChallengeType.Workout,
                DaysFromNow: -45,
                DurationDays: 31,
                Criteria: "Hoàn thành >= 20 buổi tập trong tháng 12",
                Status: ChallengeStatus.Closed,
                MaxParticipants: 200,
                RewardDescription: "Huy hiệu 'Winter Warrior' + 800 điểm",
                ImageFileName: "challenge_winter.jpg"
            ),
            new(
                Title: "Protein Power Week",
                Description: "Thử thách đạt mục tiêu protein trong 7 ngày liên tục.",
                ChallengeType: ChallengeType.Nutrition,
                DaysFromNow: -30,
                DurationDays: 7,
                Criteria: "Đạt >= 1.6g protein/kg cân nặng mỗi ngày trong 7 ngày",
                Status: ChallengeStatus.Closed,
                MaxParticipants: 100,
                RewardDescription: "Huy hiệu 'Protein Pro' + 400 điểm",
                ImageFileName: "challenge_protein.jpg"
            ),
            new(
                Title: "10K Steps Daily",
                Description: "Thử thách đi bộ 10,000 bước mỗi ngày trong 14 ngày.",
                ChallengeType: ChallengeType.Workout,
                DaysFromNow: -60,
                DurationDays: 14,
                Criteria: "Đạt >= 10,000 bước/ngày trong ít nhất 12/14 ngày",
                Status: ChallengeStatus.Closed,
                MaxParticipants: 300,
                RewardDescription: "Huy hiệu 'Step Master' + 700 điểm",
                ImageFileName: "challenge_steps.jpg"
            )
        };
    }
}

/// <summary>
/// Challenge definition for seeding.
/// </summary>
public sealed record ChallengeDefinition(
    string Title,
    string Description,
    ChallengeType ChallengeType,
    int DaysFromNow,
    int DurationDays,
    string Criteria,
    ChallengeStatus Status,
    int? MaxParticipants,
    string? RewardDescription,
    string? ImageFileName
);
