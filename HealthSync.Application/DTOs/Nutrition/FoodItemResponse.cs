namespace HealthSync.Application.DTOs.Nutrition;

/// <summary>
/// DTO đại diện cho thông tin một loại thực phẩm từ thư viện
/// (dùng khi trả về dữ liệu FoodItem cho Client)
/// </summary>
public class FoodItemResponse
{
    /// <summary>
    /// ID của mặn ăn
    /// </summary>
    public int FoodItemId { get; set; }

    /// <summary>
    /// Tên loại thực phẩm (ví dụ: "Cá hồi nướng")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Danh mục thực phẩm (ví dụ: Protein, Carbs, Fats, Vegetables, Fruits, Dairy, Beverages, Snacks, Other)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Kích cỡ khẩu phần mặc định (ví dụ: 100)
    /// </summary>
    public decimal ServingSize { get; set; }

    /// <summary>
    /// Đơn vị khẩu phần (ví dụ: g, ml, piece, cup, tbsp)
    /// </summary>
    public string ServingUnit { get; set; } = string.Empty;

    /// <summary>
    /// Calo mỗi khẩu phần
    /// </summary>
    public decimal CaloriesPerServing { get; set; }

    /// <summary>
    /// Protein (g) mỗi khẩu phần
    /// </summary>
    public decimal ProteinG { get; set; }

    /// <summary>
    /// Carbs (g) mỗi khẩu phần
    /// </summary>
    public decimal CarbsG { get; set; }

    /// <summary>
    /// Fat (g) mỗi khẩu phần
    /// </summary>
    public decimal FatG { get; set; }

    /// <summary>
    /// Chất xơ (g) - nullable, mỗi khẩu phần
    /// </summary>
    public decimal? FiberG { get; set; }

    /// <summary>
    /// Đường (g) - nullable, mỗi khẩu phần
    /// </summary>
    public decimal? SugarG { get; set; }

    /// <summary>
    /// Mô tả về loại thực phẩm
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL hình ảnh của thực phẩm (lưu trên MinIO)
    /// </summary>
    public string? ImageUrl { get; set; }
}
