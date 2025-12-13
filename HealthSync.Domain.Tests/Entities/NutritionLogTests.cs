using FluentAssertions;
using HealthSync.Domain.Entities;
using Xunit;

namespace HealthSync.Domain.Tests.Entities;

public class NutritionLogTests
{
    [Fact]
    public void NutritionLog_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var nutritionLog = new NutritionLog();

        // Assert
        nutritionLog.NutritionLogId.Should().Be(0);
        nutritionLog.UserId.Should().Be(0);
        nutritionLog.LogDate.Should().Be(default(DateTime));
        nutritionLog.TotalCalories.Should().Be(0);
        nutritionLog.TotalProteinG.Should().Be(0);
        nutritionLog.TotalCarbsG.Should().Be(0);
        nutritionLog.TotalFatG.Should().Be(0);
        nutritionLog.Notes.Should().BeNull();
        nutritionLog.CreatedAt.Should().Be(default(DateTime));
        nutritionLog.FoodEntries.Should().NotBeNull();
        nutritionLog.FoodEntries.Should().BeEmpty();
    }

    [Fact]
    public void NutritionLog_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var nutritionLog = new NutritionLog();
        var logDate = DateTime.UtcNow.Date;
        var createdAt = DateTime.UtcNow;

        // Act
        nutritionLog.NutritionLogId = 1;
        nutritionLog.UserId = 123;
        nutritionLog.LogDate = logDate;
        nutritionLog.TotalCalories = 2000.5m;
        nutritionLog.TotalProteinG = 150.0m;
        nutritionLog.TotalCarbsG = 250.0m;
        nutritionLog.TotalFatG = 80.0m;
        nutritionLog.Notes = "High protein day";
        nutritionLog.CreatedAt = createdAt;

        // Assert
        nutritionLog.NutritionLogId.Should().Be(1);
        nutritionLog.UserId.Should().Be(123);
        nutritionLog.LogDate.Should().Be(logDate);
        nutritionLog.TotalCalories.Should().Be(2000.5m);
        nutritionLog.TotalProteinG.Should().Be(150.0m);
        nutritionLog.TotalCarbsG.Should().Be(250.0m);
        nutritionLog.TotalFatG.Should().Be(80.0m);
        nutritionLog.Notes.Should().Be("High protein day");
        nutritionLog.CreatedAt.Should().Be(createdAt);
    }
}