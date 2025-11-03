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
    Other
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