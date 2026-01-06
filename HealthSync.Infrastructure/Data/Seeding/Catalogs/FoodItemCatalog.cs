using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data.Seeding.Catalogs;

/// <summary>
/// Static catalog data for Food Items.
/// Provides comprehensive food library with accurate nutritional values.
/// </summary>
public static class FoodItemCatalog
{
    /// <summary>
    /// Gets the complete food item catalog.
    /// Nutritional values are per serving size.
    /// </summary>
    public static IReadOnlyList<FoodItemDefinition> GetFoodItems()
    {
        return new List<FoodItemDefinition>
        {
            // === PROTEIN ===
            new("Ức gà nướng", "Protein", 100, ServingUnit.Gram, 165, 31, 0, 3.6m, 0, 0,
                "Thịt gà nạc, nguồn protein chất lượng cao", "chicken_breast.jpg"),
            
            new("Cá hồi nướng", "Protein", 100, ServingUnit.Gram, 208, 20, 0, 13, 0, 0,
                "Giàu omega-3, protein và vitamin D", "salmon.jpg"),
            
            new("Thịt bò nạc", "Protein", 100, ServingUnit.Gram, 250, 26, 0, 15, 0, 0,
                "Thịt bò phi lê, giàu sắt và B12", "beef_steak.jpg"),
            
            new("Trứng gà luộc", "Protein", 1, ServingUnit.Piece, 78, 6, 0.6m, 5, 0, 0.6m,
                "Trứng luộc chín, nguồn protein hoàn chỉnh", "boiled_egg.jpg"),
            
            new("Đậu phụ", "Protein", 100, ServingUnit.Gram, 76, 8, 1.9m, 4.8m, 0.3m, 0.7m,
                "Protein thực vật từ đậu nành", "tofu.jpg"),
            
            new("Tôm luộc", "Protein", 100, ServingUnit.Gram, 99, 24, 0.2m, 0.3m, 0, 0,
                "Hải sản ít calo, giàu protein", "shrimp.jpg"),

            // === CARBS / GRAINS ===
            new("Cơm trắng", "Carbs", 100, ServingUnit.Gram, 130, 2.7m, 28, 0.3m, 0.4m, 0,
                "Cơm trắng nấu chín", "white_rice.jpg"),
            
            new("Cơm gạo lứt", "Carbs", 100, ServingUnit.Gram, 123, 2.7m, 26, 1, 1.6m, 0.4m,
                "Gạo lứt giàu chất xơ và khoáng chất", "brown_rice.jpg"),
            
            new("Yến mạch", "Carbs", 40, ServingUnit.Gram, 150, 5, 27, 2.5m, 4, 0.4m,
                "Yến mạch nguyên hạt, giàu beta-glucan", "oatmeal.jpg"),
            
            new("Bánh mì nguyên cám", "Carbs", 1, ServingUnit.Piece, 80, 4, 15, 1, 2, 1.5m,
                "Bánh mì làm từ bột mì nguyên cám", "whole_grain_bread.jpg"),
            
            new("Khoai lang", "Carbs", 100, ServingUnit.Gram, 86, 1.6m, 20, 0.1m, 3, 4.2m,
                "Khoai lang luộc, giàu vitamin A", "sweet_potato.jpg"),
            
            new("Mì Ý (pasta)", "Carbs", 100, ServingUnit.Gram, 131, 5, 25, 1.1m, 1.8m, 0.6m,
                "Mì Ý luộc chín", "pasta.jpg"),

            // === VEGETABLES ===
            new("Bông cải xanh (Broccoli)", "Vegetables", 100, ServingUnit.Gram, 34, 2.8m, 7, 0.4m, 2.6m, 1.7m,
                "Rau xanh giàu vitamin C và K", "broccoli.jpg"),
            
            new("Rau chân vịt (Spinach)", "Vegetables", 100, ServingUnit.Gram, 23, 2.9m, 3.6m, 0.4m, 2.2m, 0.4m,
                "Rau xanh lá giàu sắt và folate", "spinach.jpg"),
            
            new("Cà rốt", "Vegetables", 100, ServingUnit.Gram, 41, 0.9m, 10, 0.2m, 2.8m, 4.7m,
                "Giàu beta-carotene và vitamin A", "carrot.jpg"),
            
            new("Dưa chuột", "Vegetables", 100, ServingUnit.Gram, 15, 0.7m, 3.6m, 0.1m, 0.5m, 1.7m,
                "Rau tươi ít calo, giàu nước", "cucumber.jpg"),
            
            new("Cà chua", "Vegetables", 100, ServingUnit.Gram, 18, 0.9m, 3.9m, 0.2m, 1.2m, 2.6m,
                "Giàu lycopene và vitamin C", "tomato.jpg"),

            // === FRUITS ===
            new("Chuối", "Fruits", 1, ServingUnit.Piece, 105, 1.3m, 27, 0.4m, 3.1m, 14,
                "Trái cây giàu kali và carb tự nhiên", "banana.jpg"),
            
            new("Táo", "Fruits", 1, ServingUnit.Piece, 95, 0.5m, 25, 0.3m, 4.4m, 19,
                "Trái cây giàu chất xơ và vitamin C", "apple.jpg"),
            
            new("Việt quất (Blueberry)", "Fruits", 100, ServingUnit.Gram, 57, 0.7m, 14, 0.3m, 2.4m, 10,
                "Trái cây giàu chất chống oxy hóa", "blueberry.jpg"),
            
            new("Cam", "Fruits", 1, ServingUnit.Piece, 62, 1.2m, 15, 0.2m, 3.1m, 12,
                "Trái cây giàu vitamin C", "orange.jpg"),
            
            new("Bơ (Avocado)", "Fruits", 100, ServingUnit.Gram, 160, 2, 9, 15, 7, 0.7m,
                "Trái cây giàu chất béo lành mạnh", "avocado.jpg"),

            // === DAIRY ===
            new("Sữa tươi không đường", "Dairy", 200, ServingUnit.Milliliter, 122, 8, 12, 5, 0, 12,
                "Sữa bò nguyên chất", "milk.jpg"),
            
            new("Sữa chua Hy Lạp", "Dairy", 150, ServingUnit.Gram, 100, 17, 6, 0.7m, 0, 4,
                "Sữa chua giàu protein, ít đường", "greek_yogurt.jpg"),
            
            new("Phô mai Cottage", "Dairy", 100, ServingUnit.Gram, 98, 11, 3.4m, 4.3m, 0, 2.7m,
                "Phô mai ít béo, giàu protein", "cottage_cheese.jpg"),
            
            new("Whey Protein", "Dairy", 30, ServingUnit.Gram, 120, 24, 3, 1.5m, 0, 1,
                "Bột đạm whey concentrate", "whey_protein.jpg"),

            // === FATS ===
            new("Dầu ô liu", "Fats", 1, ServingUnit.Tablespoon, 119, 0, 0, 14, 0, 0,
                "Dầu ô liu nguyên chất extra virgin", "olive_oil.jpg"),
            
            new("Hạnh nhân", "Fats", 30, ServingUnit.Gram, 164, 6, 6, 14, 3.5m, 1.2m,
                "Hạt giàu vitamin E và chất béo lành mạnh", "almonds.jpg"),
            
            new("Bơ đậu phộng", "Fats", 2, ServingUnit.Tablespoon, 188, 8, 6, 16, 2, 3,
                "Bơ đậu phộng tự nhiên", "peanut_butter.jpg"),
            
            new("Quả óc chó", "Fats", 30, ServingUnit.Gram, 185, 4.3m, 4, 18, 1.9m, 0.7m,
                "Hạt giàu omega-3 thực vật", "walnuts.jpg"),

            // === BEVERAGES ===
            new("Nước ép cam", "Beverages", 250, ServingUnit.Milliliter, 112, 1.7m, 26, 0.5m, 0.5m, 21,
                "Nước cam tươi 100%", "orange_juice.jpg"),
            
            new("Trà xanh", "Beverages", 250, ServingUnit.Milliliter, 2, 0, 0, 0, 0, 0,
                "Trà xanh không đường", "green_tea.jpg"),
            
            new("Cà phê đen", "Beverages", 250, ServingUnit.Milliliter, 5, 0.3m, 0, 0, 0, 0,
                "Cà phê đen không đường", "black_coffee.jpg"),
            
            new("Sinh tố protein", "Beverages", 300, ServingUnit.Milliliter, 250, 25, 30, 5, 3, 15,
                "Sinh tố whey với chuối và yến mạch", "protein_shake.jpg"),

            // === SNACKS ===
            new("Thanh protein", "Snacks", 1, ServingUnit.Piece, 200, 20, 22, 6, 3, 8,
                "Thanh protein bar dinh dưỡng", "protein_bar.jpg"),
            
            new("Hạt chia", "Snacks", 15, ServingUnit.Gram, 73, 2.5m, 6, 5, 5, 0,
                "Hạt chia giàu omega-3 và chất xơ", "chia_seeds.jpg"),
            
            new("Sữa chua hoa quả", "Snacks", 150, ServingUnit.Gram, 135, 5, 22, 3, 0, 18,
                "Sữa chua trộn trái cây tươi", "fruit_yogurt.jpg")
        };
    }
}

/// <summary>
/// Food item definition record for catalog data.
/// </summary>
public record FoodItemDefinition(
    string Name,
    string Category,
    decimal ServingSize,
    ServingUnit ServingUnit,
    decimal CaloriesPerServing,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG,
    decimal? FiberG,
    decimal? SugarG,
    string Description,
    string? ImageFileName = null);
