using Bogus;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data.Seeding.Catalogs;
using HealthSync.Infrastructure.Data.Seeding.Fakers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace HealthSync.Infrastructure.Data.Seeding;

/// <summary>
/// Main data seeder implementation.
/// Supports idempotent seeding for Production and CI/CD environments.
/// </summary>
public sealed class DataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly SeedSettings _settings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataSeeder> _logger;
    private readonly ImageSeeder _imageSeeder;
    private readonly Faker _faker;

    // Avatar image files for demo users
    private static readonly string[] AvatarFiles =
    {
        "avatar_01.jpg", "avatar_02.jpg", "avatar_03.jpg", "avatar_04.jpg", "avatar_05.jpg",
        "avatar_06.jpg", "avatar_07.jpg", "avatar_08.jpg", "avatar_09.jpg", "avatar_10.jpg"
    };

    // Forum post image files
    private static readonly string[] PostImageFiles =
    {
        "post_transform.jpg", "post_meal.jpg", "post_gym.jpg", "post_progress.jpg"
    };

    public DataSeeder(
        ApplicationDbContext context,
        IOptions<SeedSettings> settings,
        IConfiguration configuration,
        ILogger<DataSeeder> logger,
        ILoggerFactory loggerFactory)
    {
        _context = context;
        _settings = settings.Value;
        _configuration = configuration;
        _logger = logger;
        _faker = new Faker { Random = new Randomizer(42) }; // Deterministic seed

        // Initialize ImageSeeder with its own logger from factory
        _imageSeeder = CreateImageSeeder(loggerFactory.CreateLogger<ImageSeeder>());
    }

    private ImageSeeder CreateImageSeeder(ILogger<ImageSeeder> logger)
    {
        var endpoint = _configuration["MinIO:Endpoint"] ?? "localhost:9000";
        var accessKey = _configuration["MinIO:AccessKey"] ?? "minioadmin";
        var secretKey = _configuration["MinIO:SecretKey"] ?? "minioadmin";
        var bucket = _configuration["MinIO:BucketName"] ?? "healthsync-images";
        var useSsl = _configuration.GetValue<bool>("MinIO:UseSSL");

        var minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSsl)
            .Build();

        return new ImageSeeder(minioClient, bucket, endpoint, useSsl, _settings.SeedImagePath, logger);
    }

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database seeding...");

        // Try to acquire distributed lock to prevent race conditions when multiple instances start
        // Timeout of 0 means no wait - if another instance is seeding, we skip immediately
        await using var distributedLock = await DistributedLock.TryAcquireAsync(
            _context,
            _logger,
            "HealthSync_DataSeeding",
            timeoutMs: 0,
            cancellationToken);

        if (distributedLock == null)
        {
            _logger.LogInformation(
                "Another instance is currently seeding data. Skipping seeding on this instance.");
            return;
        }

        // Check if seeding has already been completed by checking a marker
        if (await IsSeedingCompletedAsync(cancellationToken))
        {
            _logger.LogInformation("Database has already been seeded. Skipping.");
            return;
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Always seed catalog data (static data)
            var adminId = await EnsureAdminExistsAsync(cancellationToken);
            await SeedCatalogDataAsync(adminId, cancellationToken);

            // 2. Optionally seed demo data
            if (_settings.SeedDemoData)
            {
                await SeedDemoDataAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Database seeding failed. Transaction rolled back.");
            throw new InvalidOperationException("Failed to seed database. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Checks if seeding has already been completed by verifying catalog data exists.
    /// This provides a quick check before acquiring locks and running full seeding logic.
    /// </summary>
    private async Task<bool> IsSeedingCompletedAsync(CancellationToken cancellationToken)
    {
        // Check if essential catalog data exists
        // If ForumCategories, Exercises, and FoodItems all exist, seeding is considered complete
        var hasCatalogData = await _context.ForumCategories.AnyAsync(cancellationToken)
            && await _context.Exercises.AnyAsync(cancellationToken)
            && await _context.FoodItems.AnyAsync(cancellationToken);

        // If demo data seeding is enabled, also check for demo customers
        if (_settings.SeedDemoData && hasCatalogData)
        {
            var existingCustomers = await _context.ApplicationUsers
                .AsNoTracking()
                .CountAsync(u => u.Role == "Customer", cancellationToken);

            return existingCustomers >= _settings.DemoCustomerCount;
        }

        return hasCatalogData;
    }

    /// <summary>
    /// Checks for an existing admin user ID for FK references.
    /// Returns 0 if no admin exists (does NOT create one).
    /// </summary>
    private async Task<int> EnsureAdminExistsAsync(CancellationToken cancellationToken)
    {
        var admin = await _context.ApplicationUsers
            .AsNoTracking()
            .Where(u => u.Role == "Admin")
            .Select(u => new { u.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (admin == null)
        {
            _logger.LogWarning("No admin user found for catalog data FK references.");
            return 0; // Return invalid ID - catalog seeding will be skipped
        }

        return admin.UserId;
    }

    #region Catalog Seeding

    private async Task SeedCatalogDataAsync(int adminId, CancellationToken cancellationToken)
    {
        await SeedForumCategoriesAsync(cancellationToken);
        
        // Seed catalog data even if adminId is 0 (System created)
        // Now that the Entities support nullable CreatedByAdminId, we can pass null if adminId is 0
        int? dbAdminId = adminId > 0 ? adminId : null;

        await SeedExercisesAsync(dbAdminId, cancellationToken);
        await SeedFoodItemsAsync(dbAdminId, cancellationToken);
        await SeedChallengesAsync(adminId, cancellationToken);
    }

    private async Task SeedForumCategoriesAsync(CancellationToken cancellationToken)
    {
        if (await _context.ForumCategories.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Forum categories already exist. Skipping.");
            return;
        }

        var categories = ForumCategoryCatalog.GetCategories();
        var now = DateTime.UtcNow;

        foreach (var category in categories)
        {
            category.CreatedAt = now;
            category.UpdatedAt = now;
        }

        await _context.ForumCategories.AddRangeAsync(categories, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} forum categories.", categories.Count);
    }

    private async Task SeedExercisesAsync(int? adminId, CancellationToken cancellationToken)
    {
        if (await _context.Exercises.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Exercises already exist. Skipping.");
            return;
        }

        var definitions = ExerciseCatalog.GetExercises();
        var exercises = new List<Exercise>();
        var now = DateTime.UtcNow;

        foreach (var def in definitions)
        {
            // Upload image if available
            string? imageUrl = null;
            if (!string.IsNullOrEmpty(def.ImageFileName))
            {
                imageUrl = await _imageSeeder.EnsureImageAsync(def.ImageFileName, "exercises", cancellationToken);
            }

            exercises.Add(new Exercise
            {
                Name = def.Name,
                MuscleGroup = def.MuscleGroup,
                DifficultyLevel = def.DifficultyLevel,
                Equipment = def.Equipment,
                Description = def.Description,
                Instructions = def.Instructions,
                CaloriesPerMinute = def.CaloriesPerMinute,
                ImageUrl = imageUrl,
                CreatedByAdminId = adminId,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _context.Exercises.AddRangeAsync(exercises, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} exercises.", exercises.Count);
    }

    private async Task SeedFoodItemsAsync(int? adminId, CancellationToken cancellationToken)
    {
        if (await _context.FoodItems.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Food items already exist. Skipping.");
            return;
        }

        var definitions = FoodItemCatalog.GetFoodItems();
        var foodItems = new List<FoodItem>();
        var now = DateTime.UtcNow;

        foreach (var def in definitions)
        {
            // Upload image if available
            string? imageUrl = null;
            if (!string.IsNullOrEmpty(def.ImageFileName))
            {
                imageUrl = await _imageSeeder.EnsureImageAsync(def.ImageFileName, "foods", cancellationToken);
            }

            foodItems.Add(new FoodItem
            {
                Name = def.Name,
                Category = def.Category,
                ServingSize = def.ServingSize,
                ServingUnit = def.ServingUnit,
                CaloriesPerServing = def.CaloriesPerServing,
                ProteinG = def.ProteinG,
                CarbsG = def.CarbsG,
                FatG = def.FatG,
                FiberG = def.FiberG,
                SugarG = def.SugarG,
                Description = def.Description,
                ImageUrl = imageUrl,
                CreatedByAdminId = adminId,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _context.FoodItems.AddRangeAsync(foodItems, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} food items.", foodItems.Count);
    }

    private async Task SeedChallengesAsync(int adminId, CancellationToken cancellationToken)
    {
        if (await _context.Challenges.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Challenges already exist. Skipping.");
            return;
        }

        // Challenges require an admin for CreatedByAdminId FK
        if (adminId <= 0)
        {
            _logger.LogWarning("No admin user available. Skipping challenge seeding.");
            return;
        }

        var referenceDate = DateTime.UtcNow;
        var definitions = ChallengeCatalog.GetChallenges(referenceDate);
        var challenges = new List<Challenge>();
        var now = DateTime.UtcNow;

        foreach (var def in definitions)
        {
            // Upload image if available
            string? imageUrl = null;
            if (!string.IsNullOrEmpty(def.ImageFileName))
            {
                imageUrl = await _imageSeeder.EnsureImageAsync(def.ImageFileName, "challenges", cancellationToken);
            }

            var startDate = referenceDate.AddDays(def.DaysFromNow);
            var endDate = startDate.AddDays(def.DurationDays);

            challenges.Add(new Challenge
            {
                Title = def.Title,
                Description = def.Description,
                ChallengeType = def.ChallengeType,
                StartDate = startDate,
                EndDate = endDate,
                Criteria = def.Criteria,
                Status = def.Status,
                MaxParticipants = def.MaxParticipants,
                RewardDescription = def.RewardDescription,
                ImageUrl = imageUrl,
                CreatedByAdminId = adminId,
                CreatedAt = now.AddDays(def.DaysFromNow - 7), // Created a week before start
                UpdatedAt = now
            });
        }

        await _context.Challenges.AddRangeAsync(challenges, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} community challenges.", challenges.Count);
    }

    #endregion

    #region Demo Data Seeding

    private async Task SeedDemoDataAsync(CancellationToken cancellationToken)
    {
        // Check if demo data already exists
        var existingCustomers = await _context.ApplicationUsers
            .AsNoTracking()
            .CountAsync(u => u.Role == "Customer", cancellationToken);

        if (existingCustomers >= _settings.DemoCustomerCount)
        {
            _logger.LogDebug("Demo customers already exist. Skipping.");
            return;
        }

        _logger.LogInformation("Starting demo data seeding for {Count} customers...", _settings.DemoCustomerCount);

        // Create customers
        var customers = await SeedCustomersAsync(cancellationToken);

        // Load catalog data for activity generation
        var exercises = await _context.Exercises.ToListAsync(cancellationToken);
        var foodItems = await _context.FoodItems.ToListAsync(cancellationToken);
        var categories = await _context.ForumCategories.ToListAsync(cancellationToken);
        var challenges = await _context.Challenges.ToListAsync(cancellationToken);

        // Generate activity for each customer
        foreach (var customer in customers)
        {
            await SeedUserActivityAsync(customer, exercises, foodItems, cancellationToken);
        }

        // Generate forum posts with images
        await SeedForumPostsAsync(customers, categories, cancellationToken);

        // Seed goals with progress records for customers
        await SeedGoalsAsync(customers, cancellationToken);

        // Seed challenge participations for customers
        if (challenges.Count > 0)
        {
            SeedChallengeParticipations(customers, challenges);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Calculate and update contribution points for seeded users
        await CalculateSeededUserPointsAsync(customers, cancellationToken);

        _logger.LogInformation("Demo data seeding completed.");
    }

    private async Task<List<ApplicationUser>> SeedCustomersAsync(CancellationToken cancellationToken)
    {
        var customerFaker = CustomerFaker.Create();
        var profileFaker = UserProfileFaker.Create();
        var customers = new List<ApplicationUser>();
        var now = DateTime.UtcNow;

        for (int i = 0; i < _settings.DemoCustomerCount; i++)
        {
            var customer = customerFaker.Generate();
            
            // Hash password using PBKDF2 (same as AuthService)
            customer.PasswordHash = HashPassword(_settings.DefaultCustomerPassword);

            // Generate profile
            var profile = profileFaker.Generate();
            profile.User = customer;

            // Random avatar
            var avatarFile = _faker.PickRandom(AvatarFiles);
            profile.AvatarUrl = await _imageSeeder.EnsureImageAsync(avatarFile, "avatars", cancellationToken);

            customer.UserProfile = profile;

            // Create leaderboard entry
            customer.Leaderboard = new Leaderboard
            {
                User = customer,
                TotalPoints = 0,
                UpdatedAt = now
            };

            customers.Add(customer);
        }

        await _context.ApplicationUsers.AddRangeAsync(customers, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} demo customers.", customers.Count);
        return customers;
    }

    private async Task SeedUserActivityAsync(
        ApplicationUser user,
        List<Exercise> exercises,
        List<FoodItem> foodItems,
        CancellationToken cancellationToken)
    {
        var workoutLogs = new List<WorkoutLog>();
        var nutritionLogs = new List<NutritionLog>();

        // Generate activity for configured number of days
        for (int day = 0; day < _settings.ActivityLogDays; day++)
        {
            var date = DateTime.UtcNow.AddDays(-day);

            // 70% chance of workout on any given day
            if (_faker.Random.Bool(0.7f))
            {
                var workout = WorkoutLogFaker.GenerateWithSessions(user.UserId, date, exercises, _faker);
                workoutLogs.Add(workout);
            }

            // 85% chance of nutrition log on any given day
            if (_faker.Random.Bool(0.85f))
            {
                var nutrition = NutritionLogFaker.GenerateWithEntries(user.UserId, date, foodItems, _faker);
                nutritionLogs.Add(nutrition);
            }
        }

        await _context.WorkoutLogs.AddRangeAsync(workoutLogs, cancellationToken);
        await _context.NutritionLogs.AddRangeAsync(nutritionLogs, cancellationToken);

        _logger.LogDebug("Generated {Workouts} workouts and {Nutrition} nutrition logs for user {UserId}.",
            workoutLogs.Count, nutritionLogs.Count, user.UserId);
    }

    private async Task SeedForumPostsAsync(
        List<ApplicationUser> customers,
        List<ForumCategory> categories,
        CancellationToken cancellationToken)
    {
        var allUserIds = customers.Select(c => c.UserId).ToList();
        var posts = new List<Post>();
        var replies = new List<Reply>();

        // Process each customer who decides to create a post (30% chance)
        var postsData = customers
            .Where(_ => _faker.Random.Bool(0.3f))
            .Select(customer => (customer, category: _faker.PickRandom(categories)))
            .ToList();

        foreach (var (customer, category) in postsData)
        {
            // 50% chance of post having an image
            if (_faker.Random.Bool(0.5f))
            {
                var imageFile = _faker.PickRandom(PostImageFiles);
                var imageUrl = await _imageSeeder.EnsureImageAsync(imageFile, "forum-posts", cancellationToken);
                var post = ForumPostFaker.GeneratePostWithImage(customer.UserId, category.CategoryId, imageUrl, _faker);
                posts.Add(post);
            }
            else
            {
                var (post, postReplies) = ForumPostFaker.GeneratePostWithReplies(
                    customer.UserId, category.CategoryId, allUserIds, _faker);
                posts.Add(post);
                
                // Link replies to post
                foreach (var reply in postReplies)
                {
                    reply.Post = post;
                }
                replies.AddRange(postReplies);
            }
        }

        await _context.Posts.AddRangeAsync(posts, cancellationToken);
        await _context.Replies.AddRangeAsync(replies, cancellationToken);

        _logger.LogInformation("Seeded {Posts} forum posts with {Replies} replies.", posts.Count, replies.Count);
    }

    private async Task SeedGoalsAsync(List<ApplicationUser> customers, CancellationToken cancellationToken)
    {
        var allGoals = new List<Goal>();
        var referenceDate = DateTime.UtcNow;

        foreach (var customer in customers)
        {
            // 80% of customers have goals
            if (!_faker.Random.Bool(0.8f)) continue;

            // Each customer has 1-3 goals
            var goalCount = _faker.Random.Int(1, 3);
            var goals = GoalFaker.GenerateGoalsForUser(customer.UserId, goalCount, referenceDate, _faker);
            allGoals.AddRange(goals);
        }

        if (allGoals.Count > 0)
        {
            await _context.Goals.AddRangeAsync(allGoals, cancellationToken);
            
            // Count progress records
            var totalProgressRecords = allGoals.Sum(g => g.ProgressRecords.Count);
            _logger.LogInformation(
                "Seeded {GoalCount} goals with {ProgressCount} progress records.",
                allGoals.Count,
                totalProgressRecords);
        }
    }

    private void SeedChallengeParticipations(
        List<ApplicationUser> customers,
        List<Challenge> challenges)
    {
        var participations = new List<ChallengeParticipation>();
        var now = DateTime.UtcNow;

        // Get open and closed challenges for participation
        var openChallenges = challenges.Where(c => c.Status == ChallengeStatus.Open).ToList();
        var closedChallenges = challenges.Where(c => c.Status == ChallengeStatus.Closed).ToList();

        // Get user IDs from customers
        var customerUserIds = customers.Select(c => c.UserId).ToList();

        foreach (var userId in customerUserIds)
        {
            // 60% of customers join at least one open challenge
            if (_faker.Random.Bool(0.6f) && openChallenges.Count > 0)
            {
                var challengesToJoin = _faker.PickRandom(openChallenges, _faker.Random.Int(1, Math.Min(3, openChallenges.Count)));
                
                foreach (var challenge in challengesToJoin)
                {
                    participations.Add(CreateParticipation(userId, challenge, false, now));
                }
            }

            // 40% of customers have participated in closed challenges
            if (_faker.Random.Bool(0.4f) && closedChallenges.Count > 0)
            {
                var pastChallenges = _faker.PickRandom(closedChallenges, _faker.Random.Int(1, Math.Min(2, closedChallenges.Count)));
                
                foreach (var challenge in pastChallenges)
                {
                    participations.Add(CreateParticipation(userId, challenge, true, now));
                }
            }
        }

        if (participations.Count > 0)
        {
            _context.ChallengeParticipations.AddRange(participations);
            _logger.LogInformation("Seeded {Count} challenge participations.", participations.Count);
        }
    }

    private ChallengeParticipation CreateParticipation(
        int userId,
        Challenge challenge,
        bool isClosed,
        DateTime now)
    {
        var joinedDate = challenge.StartDate.AddDays(_faker.Random.Int(0, 7));
        
        ParticipationStatus status;
        DateTime? completedAt = null;
        string? submissionText = null;
        DateTime? submittedAt = null;

        if (isClosed)
        {
            // For closed challenges, determine outcome
            var outcome = _faker.Random.Int(1, 100);
            if (outcome <= 60)
            {
                status = ParticipationStatus.Completed;
                completedAt = challenge.EndDate.AddDays(-_faker.Random.Int(0, 3));
                submissionText = _faker.PickRandom(SubmissionTexts);
                submittedAt = completedAt.Value.AddHours(-_faker.Random.Int(1, 24));
            }
            else if (outcome <= 85)
            {
                status = ParticipationStatus.Failed;
            }
            else
            {
                status = ParticipationStatus.Joined; // Didn't complete
            }
        }
        else
        {
            // For open challenges
            var progress = _faker.Random.Int(1, 100);
            if (progress <= 70)
            {
                status = ParticipationStatus.Joined;
            }
            else if (progress <= 90)
            {
                status = ParticipationStatus.PendingApproval;
                submissionText = _faker.PickRandom(SubmissionTexts);
                submittedAt = now.AddDays(-_faker.Random.Int(0, 3));
            }
            else
            {
                status = ParticipationStatus.Completed;
                completedAt = now.AddDays(-_faker.Random.Int(1, 7));
                submissionText = _faker.PickRandom(SubmissionTexts);
                submittedAt = completedAt.Value.AddHours(-_faker.Random.Int(1, 24));
            }
        }

        return new ChallengeParticipation
        {
            ChallengeId = challenge.ChallengeId,
            UserId = userId,
            JoinedDate = joinedDate,
            Status = status,
            SubmissionText = submissionText,
            SubmittedAt = submittedAt,
            CompletedAt = completedAt,
            CreatedAt = joinedDate
        };
    }

    private static readonly string[] SubmissionTexts =
    {
        "Đã hoàn thành thử thách! Cảm thấy rất tuyệt vời.",
        "Mình đã cố gắng hết sức, hy vọng đạt yêu cầu.",
        "Thử thách này giúp mình thay đổi thói quen rất nhiều.",
        "Cảm ơn thử thách đã giúp mình có động lực tập luyện!",
        "Đã đạt được mục tiêu, tiếp tục duy trì thói quen này.",
        "Khó khăn ban đầu nhưng cuối cùng cũng vượt qua được.",
        "Rất vui khi tham gia thử thách cùng mọi người."
    };

    /// <summary>
    /// Calculate and update contribution points for seeded users.
    /// Uses same formula as PointCalculationService:
    /// Points = (WorkoutLogs * 5) + (Posts * 2) + (Replies * 1) + (CompletedChallenges * 10)
    /// </summary>
    private async Task CalculateSeededUserPointsAsync(
        List<ApplicationUser> customers,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calculating contribution points for {Count} seeded users...", customers.Count);

        const int WORKOUT_LOG_POINTS = 5;
        const int FORUM_POST_POINTS = 2;
        const int FORUM_REPLY_POINTS = 1;
        const int COMPLETED_CHALLENGE_POINTS = 10;

        var userIds = customers.Select(c => c.UserId).ToList();

        // Get all workout logs for seeded users
        var workoutCounts = await _context.WorkoutLogs
            .AsNoTracking()
            .Where(w => userIds.Contains(w.UserId))
            .GroupBy(w => w.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        // Get all forum posts for seeded users
        var postCounts = await _context.Posts
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        // Get all forum replies for seeded users
        var replyCounts = await _context.Replies
            .AsNoTracking()
            .Where(r => userIds.Contains(r.UserId))
            .GroupBy(r => r.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        // Get all completed challenges for seeded users
        var completedChallengeCounts = await _context.ChallengeParticipations
            .AsNoTracking()
            .Where(cp => userIds.Contains(cp.UserId) && cp.Status == ParticipationStatus.Completed)
            .GroupBy(cp => cp.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        // Update leaderboard and user profile for each customer
        foreach (var customer in customers)
        {
            var workouts = workoutCounts.GetValueOrDefault(customer.UserId, 0);
            var posts = postCounts.GetValueOrDefault(customer.UserId, 0);
            var replies = replyCounts.GetValueOrDefault(customer.UserId, 0);
            var challenges = completedChallengeCounts.GetValueOrDefault(customer.UserId, 0);

            var totalPoints = (workouts * WORKOUT_LOG_POINTS) +
                              (posts * FORUM_POST_POINTS) +
                              (replies * FORUM_REPLY_POINTS) +
                              (challenges * COMPLETED_CHALLENGE_POINTS);

            // Update Leaderboard
            if (customer.Leaderboard != null)
            {
                customer.Leaderboard.TotalPoints = totalPoints;
                customer.Leaderboard.UpdatedAt = DateTime.UtcNow;
            }

            // Update UserProfile
            if (customer.UserProfile != null)
            {
                customer.UserProfile.ContributionPoints = totalPoints;
                customer.UserProfile.UpdatedAt = DateTime.UtcNow;
            }

            _logger.LogDebug(
                "User {UserId}: {Workouts} workouts, {Posts} posts, {Replies} replies, {Challenges} challenges = {TotalPoints} points",
                customer.UserId, workouts, posts, replies, challenges, totalPoints);
        }

        await _context.SaveChangesAsync(cancellationToken);
        
        var totalPointsSum = customers.Sum(c => c.Leaderboard?.TotalPoints ?? 0);
        _logger.LogInformation(
            "Finished calculating points for {Count} users. Total points distributed: {TotalPoints}",
            customers.Count, totalPointsSum);
    }

    #endregion

    private static string HashPassword(string password)
    {
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        return $"{Convert.ToBase64String(salt)}.{hashed}";
    }
}
