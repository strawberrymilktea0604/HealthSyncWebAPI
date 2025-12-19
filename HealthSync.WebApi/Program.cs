using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpsPolicy;
using System.Text;
using System.Collections.Generic;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Infrastructure.Repositories;
using HealthSync.Infrastructure.Services;
using FluentValidation.AspNetCore;
using FluentValidation;
using HealthSync.WebApi.Filters;
using Hangfire;
using Hangfire.SqlServer;

// ========================================
// LOAD ENVIRONMENT VARIABLES FROM .env FILES BASED ON ENVIRONMENT
// ========================================
LoadEnvironmentVariables();

// Create builder and configure
var builder = WebApplication.CreateBuilder(args);
ConfigureConfiguration(builder);
ConfigureServices(builder);

var app = builder.Build();
ConfigureMiddleware(app);
ConfigureEndpoints(app);

await app.RunAsync();

static void LoadEnvironmentVariables()
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    var envFiles = GetEnvironmentFiles(environment);

    foreach (var envFile in envFiles)
    {
        LoadEnvironmentFile(envFile);
    }
}

static List<string> GetEnvironmentFiles(string environment)
{
    var envFiles = new List<string> { ".env" }; // Always load base .env first

    // Add environment-specific .env file
    if (environment == "Development")
    {
        envFiles.Add(".env.dev");
    }
    else if (environment == "Production")
    {
        envFiles.Add(".env.prod");
    }

    return envFiles;
}

static void LoadEnvironmentFile(string envFile)
{
    var envFilePath = Path.Combine(Directory.GetCurrentDirectory(), envFile);
    if (!File.Exists(envFilePath))
    {
        // Try solution root (one level up)
        envFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", envFile);
    }

    if (File.Exists(envFilePath))
    {
        LoadEnvironmentFromFile(envFilePath);
    }
}

static void LoadEnvironmentFromFile(string envFilePath)
{
    foreach (var line in File.ReadAllLines(envFilePath))
    {
        var trimmedLine = line.Trim();

        // Skip empty lines and comments
        if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
            continue;

        var parts = trimmedLine.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var value = parts[1].Trim();

            // Set environment variable (force override for later files)
            Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
        }
    }
}

static void ConfigureConfiguration(WebApplicationBuilder builder)
{
    // Override appsettings.json with environment variables if they exist
    OverrideConfigurationFromEnvironment(builder, "AdminInitialization:SecretKey", "ADMIN_INITIALIZATION_KEY");

    // Override JWT settings from environment variables
    OverrideConfigurationFromEnvironment(builder, "JwtSettings:SecretKey", "JWT_SECRET_KEY");
    OverrideConfigurationFromEnvironment(builder, "JwtSettings:Issuer", "JWT_ISSUER");
    OverrideConfigurationFromEnvironment(builder, "JwtSettings:Audience", "JWT_AUDIENCE");

    // Override MinIO settings from environment variables
    OverrideConfigurationFromEnvironment(builder, "MinIO:Endpoint", "MINIO_ENDPOINT");
    OverrideConfigurationFromEnvironment(builder, "MinIO:AccessKey", "MINIO_ACCESS_KEY");
    OverrideConfigurationFromEnvironment(builder, "MinIO:SecretKey", "MINIO_SECRET_KEY");
    OverrideConfigurationFromEnvironment(builder, "MinIO:BucketName", "MINIO_BUCKET_NAME");
    OverrideConfigurationFromEnvironment(builder, "MinIO:UseSSL", "MINIO_USE_SSL");
}

static void OverrideConfigurationFromEnvironment(WebApplicationBuilder builder, string configKey, string envVar)
{
    var envValue = Environment.GetEnvironmentVariable(envVar);
    if (!string.IsNullOrEmpty(envValue))
    {
        builder.Configuration[configKey] = envValue;
    }
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    // Add services to the container.
    builder.Services.AddControllers();

    // Add Health Checks
    builder.Services.AddHealthChecks();

    // Add FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<HealthSync.Application.Validators.Users.UpdateUserProfileValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<HealthSync.Application.Validators.Exercises.CreateExerciseRequestValidator>();

    // Add DbContext (without Identity)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add Hangfire services
    var hangfireConn = builder.Configuration.GetConnectionString("HangfireConnection");
    builder.Services.AddHangfire(config => config
        .UseSqlServerStorage(hangfireConn, new Hangfire.SqlServer.SqlServerStorageOptions
        {
            PrepareSchemaIfNecessary = true, // create Hangfire tables when the app runs
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
    builder.Services.AddHangfireServer();

    // Add Authentication (JWT only, no Identity)
    Console.WriteLine($"[JWT DEBUG] SecretKey: {builder.Configuration["JwtSettings:SecretKey"]?.Substring(0, 20)}...");
    Console.WriteLine($"[JWT DEBUG] Issuer: {builder.Configuration["JwtSettings:Issuer"]}");
    Console.WriteLine($"[JWT DEBUG] Audience: {builder.Configuration["JwtSettings:Audience"]}");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? "default-secret-key")),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                NameClaimType = "sub",  // ← ADD THIS LINE
                RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            };
        });

    // Add Authorization
    builder.Services.AddAuthorization();

    // Configure HTTPS redirection to prevent warnings in production
    builder.Services.Configure<HttpsRedirectionOptions>(options =>
    {
        options.HttpsPort = null; // Disable HTTPS redirection
    });

    // Register application services
    RegisterApplicationServices(builder.Services);

    // Add Swagger/OpenAPI
    ConfigureSwagger(builder.Services);
}

static void RegisterApplicationServices(IServiceCollection services)
{
    services.AddScoped<HealthSync.Application.Features.Auth.Interfaces.IAuthService, HealthSync.Application.Features.Auth.Services.AuthService>();
    services.AddScoped<HealthSync.Application.Interfaces.IUserProfileService, HealthSync.Application.Features.Users.Services.UserProfileService>();
    services.AddScoped<HealthSync.Application.Interfaces.IUserService, HealthSync.Application.Services.UserService>();
    services.AddScoped<HealthSync.Application.Interfaces.IUserRepository, HealthSync.Infrastructure.Repositories.UserRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.IUserProfileRepository, HealthSync.Infrastructure.Repositories.UserProfileRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.ILeaderboardRepository, HealthSync.Infrastructure.Repositories.LeaderboardRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.ILeaderboardService, HealthSync.Application.Services.LeaderboardService>();
    services.AddScoped<HealthSync.Application.Interfaces.IJwtService, HealthSync.Infrastructure.Services.JwtService>();
    services.AddScoped<HealthSync.Application.Interfaces.IExerciseService, HealthSync.Application.Services.ExerciseService>();
    services.AddScoped<HealthSync.Application.Interfaces.IExerciseRepository, HealthSync.Infrastructure.Repositories.ExerciseRepository>();
    // Register storage service (MinIO) - implements both IStorageService and IFileStorageService
    services.AddSingleton<HealthSync.Infrastructure.Services.MinioService>();
    services.AddSingleton<HealthSync.Application.Interfaces.IStorageService>(sp => sp.GetRequiredService<HealthSync.Infrastructure.Services.MinioService>());
    services.AddSingleton<HealthSync.Application.Interfaces.IFileStorageService>(sp => sp.GetRequiredService<HealthSync.Infrastructure.Services.MinioService>());
    services.AddScoped<HealthSync.Application.Interfaces.IFoodItemRepository, HealthSync.Infrastructure.Repositories.FoodItemRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.IWorkoutLogRepository, HealthSync.Infrastructure.Repositories.WorkoutLogRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.IWorkoutLogService, HealthSync.Application.Services.WorkoutLogService>();
    services.AddScoped<HealthSync.Application.Interfaces.INutritionLogService, HealthSync.Application.Services.NutritionLogService>();
    services.AddScoped<HealthSync.Application.Interfaces.INutritionLogRepository, HealthSync.Infrastructure.Repositories.NutritionLogRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.IForumPostRepository, HealthSync.Infrastructure.Repositories.ForumPostRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.IForumReplyRepository, HealthSync.Infrastructure.Repositories.ForumReplyRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.IForumCategoryRepository, HealthSync.Infrastructure.Repositories.ForumCategoryRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.IForumAdminService, HealthSync.Application.Services.ForumAdminService>();
    services.AddScoped<HealthSync.Application.Interfaces.IChallengeRepository, HealthSync.Infrastructure.Repositories.ChallengeRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.IChallengeParticipationRepository, HealthSync.Infrastructure.Repositories.ChallengeParticipationRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.INotificationRepository, HealthSync.Infrastructure.Repositories.NotificationRepository>();
    services.AddScoped<HealthSync.Application.Interfaces.IChallengeAdminService, HealthSync.Application.Services.ChallengeAdminService>();
    services.AddScoped<HealthSync.Application.Interfaces.IChallengeParticipationService, HealthSync.Application.Services.ChallengeParticipationService>();
    services.AddScoped<HealthSync.Application.Interfaces.IDashboardAdminService, HealthSync.Application.Services.DashboardAdminService>();
    services.AddScoped<HealthSync.Application.Services.UserDependencies>(sp => new HealthSync.Application.Services.UserDependencies(
        sp.GetRequiredService<HealthSync.Application.Interfaces.IUserRepository>(),
        sp.GetRequiredService<HealthSync.Application.Interfaces.IUserProfileRepository>()
    ));
    services.AddScoped<HealthSync.Application.Services.WorkoutDependencies>(sp => new HealthSync.Application.Services.WorkoutDependencies(
        sp.GetRequiredService<HealthSync.Application.Interfaces.IWorkoutLogRepository>(),
        sp.GetRequiredService<HealthSync.Application.Interfaces.IExerciseRepository>(),
        sp.GetRequiredService<HealthSync.Application.Interfaces.IExerciseSessionRepository>()
    ));
    services.AddScoped<HealthSync.Application.Services.ForumDependencies>(sp => new HealthSync.Application.Services.ForumDependencies(
        sp.GetRequiredService<HealthSync.Application.Interfaces.IForumPostRepository>(),
        sp.GetRequiredService<HealthSync.Application.Interfaces.IForumReplyRepository>(),
        sp.GetRequiredService<HealthSync.Application.Interfaces.IForumCategoryRepository>()
    ));
    services.AddScoped<HealthSync.Application.Services.ChallengeDependencies>(sp => new HealthSync.Application.Services.ChallengeDependencies(
        sp.GetRequiredService<HealthSync.Application.Interfaces.IChallengeRepository>(),
        sp.GetRequiredService<HealthSync.Application.Interfaces.IChallengeParticipationRepository>()
    ));
    services.AddScoped<HealthSync.Application.Services.DashboardDependencies>(sp => new HealthSync.Application.Services.DashboardDependencies(
        sp.GetRequiredService<HealthSync.Application.Services.UserDependencies>(),
        sp.GetRequiredService<HealthSync.Application.Services.WorkoutDependencies>(),
        sp.GetRequiredService<HealthSync.Application.Interfaces.INutritionLogRepository>(),
        sp.GetRequiredService<HealthSync.Application.Services.ForumDependencies>(),
        sp.GetRequiredService<HealthSync.Application.Services.ChallengeDependencies>()
    ));
    services.AddScoped<HealthSync.Application.Interfaces.IExerciseSessionRepository, HealthSync.Infrastructure.Repositories.ExerciseSessionRepository>();

    // Register background jobs
    services.AddScoped<HealthSync.Application.Interfaces.ILeaderboardUpdateJob, HealthSync.Infrastructure.BackgroundJobs.LeaderboardUpdateJob>();
    services.AddScoped<HealthSync.Application.Interfaces.IPointCalculationService, HealthSync.Application.Services.PointCalculationService>();
}

static void ConfigureSwagger(IServiceCollection services)
{
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "HealthSync API", Version = "v1" });

        // Support for file uploads
        c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
        c.MapType<IEnumerable<IFormFile>>(() => new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema { Type = "string", Format = "binary" }
        });

        // Add JWT Authentication
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Support for file upload in Swagger
        c.OperationFilter<FileUploadOperationFilter>();
    });
}

static void ConfigureMiddleware(WebApplication app)
{
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        // Enable detailed error pages in development
        if (app.Configuration.GetValue<bool>("DevelopmentSettings:EnableDetailedErrors"))
        {
            app.UseDeveloperExceptionPage();
        }
    }

    // HTTPS redirection - only enable in Development, disabled in Production since NGINX handles SSL
    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    // Add Health Check endpoint
    app.MapHealthChecks("/health");

    // Add Hangfire Dashboard (protected with authorization)
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
    });

    app.UseAuthentication();
    app.UseAuthorization();
}

static void ConfigureEndpoints(WebApplication app)
{
    app.MapControllers();

    // Add root endpoint
    app.MapGet("/", () => Results.Redirect("/swagger"));

    // ========================================
    // MIGRATION HANDLING
    // ========================================
    // PRODUCTION: Migrations được chạy bởi init container riêng biệt (Dockerfile.migration)
    // Điều này tránh race condition khi chạy nhiều replicas
    // Chỉ verify database connection ở đây, KHÔNG chạy migrations tự động

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Chỉ kiểm tra kết nối database, KHÔNG chạy migrations
            // Migrations được xử lý bởi migration init container
            var canConnect = context.Database.CanConnect();

            if (canConnect)
            {
                logger.LogInformation("✓ Database connection verified successfully");
            }
            else
            {
                logger.LogWarning("⚠ Cannot connect to database - waiting for migrations to complete");
            }
        }
        catch (Exception ex)
        {
            // Log lỗi nhưng KHÔNG crash app
            logger.LogError(ex, "Database connection check failed. App will continue but may not work properly.");
        }
    }

    // Configure Hangfire Recurring Jobs
    RecurringJob.AddOrUpdate<ILeaderboardUpdateJob>(
        "update-leaderboard-points",
        job => job.UpdateUserContributionPointsAsync(),
        Cron.Daily(2), // Run daily at 2:00 AM
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });
}

namespace HealthSync.WebApi
{
    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
