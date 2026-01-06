using Bogus;
using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data.Seeding.Fakers;

/// <summary>
/// Bogus faker for Post and Reply entities.
/// Generates realistic forum content for demo purposes.
/// </summary>
public sealed class ForumPostFaker
{
    // Private constructor to prevent instantiation
    private ForumPostFaker() { }

    private static readonly string[] PostTitles =
    {
        "Hành trình giảm 10kg trong 3 tháng của mình!",
        "Chia sẻ thực đơn Eat Clean cho người mới bắt đầu",
        "Hỏi về form Squat đúng cách",
        "Review máy chạy bộ tại nhà dưới 10 triệu",
        "Cách tính TDEE và Macros chuẩn nhất?",
        "30 ngày plank challenge - Kết quả bất ngờ!",
        "Tập gym buổi sáng hay buổi tối tốt hơn?",
        "Meal prep cho cả tuần - Tiết kiệm thời gian",
        "Bị đau lưng khi deadlift, cần advice!",
        "Từ 80kg xuống 65kg - Câu chuyện của tôi",
        "So sánh Whey Protein: Optimum vs MyProtein",
        "Cách vượt qua plateau trong giảm cân",
        "Tập cardio bao lâu thì hiệu quả?",
        "Chia sẻ playlist nhạc tập gym",
        "Đau cơ sau tập - Cách phục hồi nhanh",
        "Chế độ ăn cho người muốn tăng cơ",
        "Tips tập chân cho người mới",
        "Intermittent Fasting có hiệu quả không?",
        "Góp ý về lịch tập 5 ngày/tuần",
        "Cách giữ động lực tập luyện lâu dài"
    };

    private static readonly string[] PostContents =
    {
        "Mình bắt đầu hành trình giảm cân từ 3 tháng trước. Ban đầu cân nặng 75kg, bây giờ xuống còn 65kg. Chia sẻ với mọi người những gì mình đã làm được...\n\n1. Tập luyện đều đặn 4-5 buổi/tuần\n2. Theo dõi calories bằng app\n3. Ngủ đủ giấc 7-8 tiếng\n4. Uống đủ nước\n\nKết quả ngoài mong đợi! Ai có câu hỏi gì cứ hỏi nhé.",
        
        "Sau 1 năm nghiên cứu và thực hành, mình muốn chia sẻ thực đơn Eat Clean hiệu quả:\n\n**Sáng:** Yến mạch + trứng + trái cây\n**Trưa:** Cơm gạo lứt + ức gà + rau xanh\n**Tối:** Cá hồi + khoai lang + salad\n\nNhớ uống nhiều nước và chia nhỏ bữa ăn nhé!",
        
        "Mình mới tập gym được 2 tháng, đang gặp khó khăn với bài Squat. Mỗi lần squat xong đều bị đau gối. Có ai có kinh nghiệm không ạ? Mình đang squat 40kg.\n\nCảm ơn mọi người!",
        
        "Vừa mua máy chạy bộ XYZ với giá 8 triệu. Review nhanh:\n\n**Ưu điểm:**\n- Chạy êm, không ồn\n- Màn hình hiển thị đầy đủ\n- Có chế độ nghiêng\n\n**Nhược điểm:**\n- Hơi nhỏ với người cao trên 1m75\n- Lắp ráp khó\n\nNhìn chung với mức giá này thì ổn!",
        
        "Đang chuẩn bị tham gia challenge 30 ngày của app. Mọi người có tips gì để hoàn thành không? Lần đầu tham gia nên hơi lo lắng 😅"
    };

    private static readonly string[] ReplyContents =
    {
        "Bài viết hay quá! Cảm ơn bạn đã chia sẻ. Mình cũng đang trong hành trình tương tự.",
        "Mình đã áp dụng và thấy hiệu quả. Thanks!",
        "Form của bạn có vẻ chưa chuẩn. Thử hạ mông xuống thấp hơn xem.",
        "Đồng ý với bạn. Mình cũng dùng sản phẩm này và rất hài lòng.",
        "Cố lên bạn! Consistency is key 💪",
        "Mình nghĩ bạn nên giảm cường độ một chút để tránh chấn thương.",
        "Great progress! Keep going!",
        "Có thể cho mình xin thêm chi tiết không?",
        "Mình đã thử cách này nhưng không phù hợp với cơ địa mình.",
        "Recommend thêm bài bench press nữa nhé!",
        "Góp ý nhỏ: nên warm up kỹ hơn trước khi tập nặng.",
        "Cảm ơn tip! Sẽ thử ngay.",
        "Đỉnh quá! Motivation cho mình 🔥",
        "Mình cũng gặp vấn đề tương tự. Sau khi chỉnh form thì đỡ hơn nhiều.",
        "Chia sẻ rất hữu ích. Saved!"
    };

    public static (Post post, List<Reply> replies) GeneratePostWithReplies(
        int userId,
        int categoryId,
        IReadOnlyList<int> allUserIds,
        Faker faker)
    {
        var post = new Post
        {
            UserId = userId,
            CategoryId = categoryId,
            Title = faker.PickRandom(PostTitles),
            Content = faker.PickRandom(PostContents),
            IsPinned = false,
            IsLocked = false,
            CreatedAt = faker.Date.Past(1, DateTime.UtcNow.AddDays(-1)),
            UpdatedAt = DateTime.UtcNow
        };

        var replies = new List<Reply>();

        // 70% chance to have replies
        if (faker.Random.Bool(0.7f))
        {
            var replyCount = faker.Random.Int(1, 8);
            var baseTime = post.CreatedAt;

            for (int i = 0; i < replyCount; i++)
            {
                // Random user from pool (can be same or different from post author)
                var replyUserId = allUserIds[faker.Random.Int(0, allUserIds.Count - 1)];
                baseTime = baseTime.AddMinutes(faker.Random.Int(5, 180));

                var reply = new Reply
                {
                    UserId = replyUserId,
                    Content = faker.PickRandom(ReplyContents),
                    IsHidden = false,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime
                };

                replies.Add(reply);
            }
        }

        return (post, replies);
    }

    public static Post GeneratePostWithImage(
        int userId,
        int categoryId,
        string? imageUrl,
        Faker faker)
    {
        var imageTitles = new[]
        {
            "Check-in phòng tập hôm nay! 💪",
            "Transformation sau 6 tháng tập luyện",
            "Meal prep cuối tuần của mình",
            "New PR deadlift 100kg!",
            "Progress pic - Tháng 1 vs Tháng 6"
        };

        var imageContents = new[]
        {
            "Chia sẻ với mọi người kết quả sau thời gian kiên trì tập luyện. Không có gì là không thể nếu chúng ta quyết tâm!\n\nCảm ơn cộng đồng HealthSync đã luôn động viên. 🙏",
            "Hôm nay đã hoàn thành buổi tập khá nặng. Feeling good! Ai ở đây cũng tập hôm nay không?",
            "Dành ngày Chủ nhật để chuẩn bị đồ ăn cho cả tuần. Meal prep giúp tiết kiệm thời gian và kiểm soát calo tốt hơn nhiều.\n\nMenu tuần này: Ức gà + Cơm gạo lứt + Rau xào.",
            "Finally hit my PR! 🎉 Tập luyện đúng cách và kiên nhẫn chắc chắn sẽ có kết quả.",
            "6 tháng trước mình bắt đầu từ số 0. Giờ nhìn lại thấy sự thay đổi rõ rệt. Never give up!"
        };

        var index = faker.Random.Int(0, imageTitles.Length - 1);

        return new Post
        {
            UserId = userId,
            CategoryId = categoryId,
            Title = imageTitles[index],
            Content = imageContents[index],
            ImageUrl = imageUrl,
            IsPinned = false,
            IsLocked = false,
            CreatedAt = faker.Date.Past(1, DateTime.UtcNow.AddDays(-1)),
            UpdatedAt = DateTime.UtcNow
        };
    }
}
