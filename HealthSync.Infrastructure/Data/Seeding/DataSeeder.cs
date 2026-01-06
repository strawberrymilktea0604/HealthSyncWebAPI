using Bogus;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data.Seeding.Catalogs;
using HealthSync.Infrastructure.Data.Seeding.Fakers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;

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
    /// Ensures at least one admin exists for FK references.
    /// Does NOT create admin if none exists - returns first admin ID.
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
            _logger.LogWarning("No admin user found. Catalog data will use placeholder admin ID = 1.");
            return 1; // Placeholder - catalog items will reference this
        }

        return admin.UserId;
    }

    #region Catalog Seeding

    private async Task SeedCatalogDataAsync(int adminId, CancellationToken cancellationToken)
    {
        await SeedForumCategoriesAsync(cancellationToken);
        await SeedExercisesAsync(adminId, cancellationToken);
        await SeedFoodItemsAsync(adminId, cancellationToken);
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

    private async Task SeedExercisesAsync(int adminId, CancellationToken cancellationToken)
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

    private async Task SeedFoodItemsAsync(int adminId, CancellationToken cancellationToken)
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

        // Generate activity for each customer
        foreach (var customer in customers)
        {
            await SeedUserActivityAsync(customer, exercises, foodItems, cancellationToken);
        }

        // Generate forum posts with images
        await SeedForumPostsAsync(customers, categories, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
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
            
            // Hash password using BCrypt
            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(_settings.DefaultCustomerPassword);

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

    #endregion
}
