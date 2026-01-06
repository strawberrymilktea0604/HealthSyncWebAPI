using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data.Seeding.Catalogs;

/// <summary>
/// Static catalog data for Forum Categories.
/// Provides idempotent seed data.
/// </summary>
public static class ForumCategoryCatalog
{
    public static IReadOnlyList<ForumCategory> GetCategories()
    {
        return new List<ForumCategory>
        {
            new()
            {
                Name = "Góc Chia Sẻ",
                Description = "Nơi chia sẻ hành trình thay đổi bản thân, kết quả luyện tập",
                DisplayOrder = 1
            },
            new()
            {
                Name = "Hỏi Đáp Dinh Dưỡng",
                Description = "Thắc mắc về Eat Clean, Keto, Macros, chế độ ăn uống khoa học",
                DisplayOrder = 2
            },
            new()
            {
                Name = "Kỹ Thuật Tập Luyện",
                Description = "Chỉnh sửa form bài tập, chia sẻ kinh nghiệm tập luyện",
                DisplayOrder = 3
            },
            new()
            {
                Name = "Review Dụng Cụ",
                Description = "Đánh giá thiết bị tập luyện, quần áo thể thao, phụ kiện",
                DisplayOrder = 4
            },
            new()
            {
                Name = "Thử Thách Cộng Đồng",
                Description = "Thảo luận về các thử thách, chia sẻ tiến độ hoàn thành",
                DisplayOrder = 5
            }
        };
    }
}
