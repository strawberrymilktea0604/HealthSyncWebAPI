namespace HealthSync.Domain.Entities;

public enum MuscleGroup
{
    Chest,
    Back,
    Legs,
    Shoulders,
    Arms,
    Core,
    Cardio,
    FullBody
}

public enum DifficultyLevel
{
    Beginner,
    Intermediate,
    Advanced
}

public enum Equipment
{
    Barbell,
    Dumbbell,
    Machine,
    Bodyweight,
    Cable,
    Kettlebell,
    ResistanceBand,
    Other
}

public enum FoodCategory
{
    Protein,
    Carbs,
    Fats,
    Vegetables,
    Fruits,
    Dairy,
    Beverages,
    Snacks,
    Other,
    Grains
}

public enum MealType
{
    Breakfast,
    Lunch,
    Dinner,
    Snack
}

public enum ChallengeType
{
    Workout,
    Nutrition,
    Hybrid
}

public enum ChallengeStatus
{
    Open,
    Closed
}

public enum ParticipationStatus
{
    Joined,
    PendingApproval,
    Completed,
    Failed
}

public enum GoalType
{
    WeightLoss,
    WeightGain,
    MaintainWeight,
    BodyMeasurement
}

public enum ActivityLevel
{
    Sedentary,
    LightlyActive,
    ModeratelyActive,
    VeryActive,
    ExtraActive
}

public enum Gender
{
    Male,
    Female,
    Other
}

public enum GoalStatus
{
    InProgress,
    Completed,
    Cancelled
}

public enum ServingUnit
{
    Gram,      // g
    Milliliter, // ml
    Piece,
    Cup,
    Tablespoon, // tbsp
    Teaspoon    // tsp
}