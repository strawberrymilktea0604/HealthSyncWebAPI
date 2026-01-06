using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data.Seeding.Catalogs;

/// <summary>
/// Static catalog data for Exercises.
/// Provides comprehensive exercise library for fitness tracking.
/// </summary>
public static class ExerciseCatalog
{
    /// <summary>
    /// Gets the complete exercise catalog with optional image URLs.
    /// adminId is used for CreatedByAdminId field.
    /// </summary>
    public static IReadOnlyList<ExerciseDefinition> GetExercises()
    {
        return new List<ExerciseDefinition>
        {
            // === CHEST ===
            new("Barbell Bench Press", MuscleGroup.Chest, DifficultyLevel.Intermediate, Equipment.Barbell,
                "Classic compound chest exercise targeting pectorals, triceps, and front deltoids.",
                "Lie on bench with feet flat. Grip bar slightly wider than shoulder width. Lower to chest, press up.", 
                8.0m, "bench_press.jpg"),
            
            new("Push Up", MuscleGroup.Chest, DifficultyLevel.Beginner, Equipment.Bodyweight,
                "Fundamental bodyweight exercise for chest, shoulders, and triceps.",
                "Start in plank position. Lower body until chest nearly touches floor. Push back up.",
                5.0m, "push_up.jpg"),
            
            new("Dumbbell Fly", MuscleGroup.Chest, DifficultyLevel.Intermediate, Equipment.Dumbbell,
                "Isolation exercise targeting chest muscles with emphasis on stretch.",
                "Lie on bench, extend arms above chest. Lower weights in arc motion, feel stretch, return.",
                4.0m, "dumbbell_fly.jpg"),
            
            new("Incline Dumbbell Press", MuscleGroup.Chest, DifficultyLevel.Intermediate, Equipment.Dumbbell,
                "Targets upper chest muscles with inclined bench position.",
                "Set bench at 30-45 degrees. Press dumbbells from shoulder level to full extension.",
                7.0m, "incline_press.jpg"),

            // === BACK ===
            new("Barbell Deadlift", MuscleGroup.Back, DifficultyLevel.Advanced, Equipment.Barbell,
                "Compound exercise targeting entire posterior chain.",
                "Stand with feet hip-width. Grip bar, keep back straight. Lift by extending hips and knees.",
                10.0m, "deadlift.jpg"),
            
            new("Pull Up", MuscleGroup.Back, DifficultyLevel.Intermediate, Equipment.Bodyweight,
                "Classic bodyweight exercise for back and biceps.",
                "Hang from bar with overhand grip. Pull body up until chin clears bar. Lower with control.",
                6.0m, "pull_up.jpg"),
            
            new("Barbell Row", MuscleGroup.Back, DifficultyLevel.Intermediate, Equipment.Barbell,
                "Compound back exercise targeting lats and rhomboids.",
                "Bend at hips, keep back flat. Pull bar to lower chest, squeeze shoulder blades.",
                7.0m, "barbell_row.jpg"),
            
            new("Lat Pulldown", MuscleGroup.Back, DifficultyLevel.Beginner, Equipment.Cable,
                "Machine exercise mimicking pull-up motion for lat development.",
                "Sit at machine, grip bar wide. Pull down to upper chest, control return.",
                5.0m, "lat_pulldown.jpg"),

            // === LEGS ===
            new("Barbell Squat", MuscleGroup.Legs, DifficultyLevel.Intermediate, Equipment.Barbell,
                "Compound exercise targeting quadriceps, glutes, and hamstrings.",
                "Bar on upper back. Feet shoulder-width. Squat until thighs parallel, drive up through heels.",
                9.0m, "squat.jpg"),
            
            new("Leg Press", MuscleGroup.Legs, DifficultyLevel.Beginner, Equipment.Machine,
                "Machine-based leg exercise with lower back support.",
                "Sit in machine, feet shoulder-width on platform. Lower weight, press back without locking knees.",
                6.0m, "leg_press.jpg"),
            
            new("Romanian Deadlift", MuscleGroup.Legs, DifficultyLevel.Intermediate, Equipment.Barbell,
                "Hamstring-focused deadlift variation with emphasis on stretch.",
                "Hold bar at hips. Push hips back, lower bar along legs. Feel hamstring stretch, return.",
                7.0m, "romanian_deadlift.jpg"),
            
            new("Walking Lunge", MuscleGroup.Legs, DifficultyLevel.Beginner, Equipment.Bodyweight,
                "Dynamic leg exercise improving balance and unilateral strength.",
                "Step forward into lunge. Front knee 90 degrees. Push off to next step.",
                5.0m, "walking_lunge.jpg"),

            // === SHOULDERS ===
            new("Overhead Press", MuscleGroup.Shoulders, DifficultyLevel.Intermediate, Equipment.Barbell,
                "Compound shoulder exercise for overall deltoid development.",
                "Bar at shoulder level. Press overhead until arms locked. Lower with control.",
                6.0m, "overhead_press.jpg"),
            
            new("Lateral Raise", MuscleGroup.Shoulders, DifficultyLevel.Beginner, Equipment.Dumbbell,
                "Isolation exercise targeting lateral deltoid heads.",
                "Hold dumbbells at sides. Raise arms to shoulder level, slight bend in elbows.",
                3.0m, "lateral_raise.jpg"),
            
            new("Face Pull", MuscleGroup.Shoulders, DifficultyLevel.Beginner, Equipment.Cable,
                "Rear deltoid and rotator cuff exercise for shoulder health.",
                "Attach rope to cable. Pull towards face, separate rope ends, squeeze rear delts.",
                4.0m, "face_pull.jpg"),

            // === ARMS ===
            new("Barbell Curl", MuscleGroup.Arms, DifficultyLevel.Beginner, Equipment.Barbell,
                "Classic bicep isolation exercise.",
                "Stand with bar at arms length. Curl weight to shoulders, lower with control.",
                4.0m, "barbell_curl.jpg"),
            
            new("Tricep Pushdown", MuscleGroup.Arms, DifficultyLevel.Beginner, Equipment.Cable,
                "Tricep isolation using cable machine.",
                "Grip bar/rope at chest level. Push down until arms straight, control return.",
                3.0m, "tricep_pushdown.jpg"),
            
            new("Hammer Curl", MuscleGroup.Arms, DifficultyLevel.Beginner, Equipment.Dumbbell,
                "Bicep and brachialis exercise with neutral grip.",
                "Hold dumbbells with neutral grip. Curl to shoulders keeping palms facing inward.",
                4.0m, "hammer_curl.jpg"),

            // === CORE ===
            new("Plank", MuscleGroup.Core, DifficultyLevel.Beginner, Equipment.Bodyweight,
                "Isometric core exercise for overall stability.",
                "Hold push-up position on forearms. Keep body straight, engage core.",
                3.0m, "plank.jpg"),
            
            new("Russian Twist", MuscleGroup.Core, DifficultyLevel.Intermediate, Equipment.Bodyweight,
                "Rotational core exercise targeting obliques.",
                "Sit with knees bent, lean back slightly. Rotate torso side to side.",
                4.0m, "russian_twist.jpg"),
            
            new("Hanging Leg Raise", MuscleGroup.Core, DifficultyLevel.Advanced, Equipment.Bodyweight,
                "Advanced ab exercise for lower abdominal development.",
                "Hang from bar. Raise legs to parallel or higher, lower with control.",
                5.0m, "leg_raise.jpg"),

            // === CARDIO ===
            new("Treadmill Running", MuscleGroup.Cardio, DifficultyLevel.Beginner, Equipment.Machine,
                "Indoor running on treadmill for cardiovascular fitness.",
                "Set speed and incline. Maintain proper running form. Start with walking warmup.",
                10.0m, "treadmill.jpg"),
            
            new("Rowing Machine", MuscleGroup.Cardio, DifficultyLevel.Intermediate, Equipment.Machine,
                "Full body cardio exercise engaging legs, back, and arms.",
                "Sit on rower, feet secured. Drive with legs, lean back, pull handle to chest.",
                12.0m, "rowing.jpg"),
            
            new("Jump Rope", MuscleGroup.Cardio, DifficultyLevel.Beginner, Equipment.Other,
                "High-intensity cardio improving coordination and footwork.",
                "Hold rope handles. Jump as rope passes under feet. Land softly on balls of feet.",
                11.0m, "jump_rope.jpg"),
            
            new("Burpee", MuscleGroup.FullBody, DifficultyLevel.Intermediate, Equipment.Bodyweight,
                "High-intensity full body exercise combining squat, push-up, and jump.",
                "Squat down, hands on floor. Jump feet back, do push-up. Jump feet forward, jump up.",
                12.0m, "burpee.jpg")
        };
    }
}

/// <summary>
/// Exercise definition record for catalog data.
/// </summary>
public record ExerciseDefinition(
    string Name,
    MuscleGroup MuscleGroup,
    DifficultyLevel DifficultyLevel,
    Equipment Equipment,
    string Description,
    string Instructions,
    decimal CaloriesPerMinute,
    string? ImageFileName = null);
