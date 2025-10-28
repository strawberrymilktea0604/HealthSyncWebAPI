using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for domain entities
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Goal> Goals { get; set; }
    public DbSet<ProgressRecord> ProgressRecords { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<WorkoutLog> WorkoutLogs { get; set; }
    public DbSet<ExerciseSession> ExerciseSessions { get; set; }
    public DbSet<FoodItem> FoodItems { get; set; }
    public DbSet<NutritionLog> NutritionLogs { get; set; }
    public DbSet<FoodEntry> FoodEntries { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<ChallengeParticipation> ChallengeParticipations { get; set; }
    public DbSet<CommunityChallenge> CommunityChallenges { get; set; }
    public DbSet<UserChallengeSubmission> UserChallengeSubmissions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure entities
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}