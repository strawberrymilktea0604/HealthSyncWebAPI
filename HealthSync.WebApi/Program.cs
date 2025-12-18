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
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
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

foreach (var envFile in envFiles)
{
    var envFilePath = Path.Combine(Directory.GetCurrentDirectory(), envFile);
    if (!File.Exists(envFilePath))
    {
        // Try solution root (one level up)
        envFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", envFile);
    }

    if (File.Exists(envFilePath))
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
}

var builder = WebApplication.CreateBuilder(args);

// Configure URLs for Docker
builder.WebHost.UseUrls("http://0.0.0.0:8080");

// Override appsettings.json with environment variables if they exist
var adminKeyFromEnv = Environment.GetEnvironmentVariable("ADMIN_INITIALIZATION_KEY");
if (!string.IsNullOrEmpty(adminKeyFromEnv))
{
    builder.Configuration["AdminInitialization:SecretKey"] = adminKeyFromEnv;
}

// Override JWT settings from environment variables
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_SECRET_KEY")))
    builder.Configuration["JwtSettings:SecretKey"] = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_ISSUER")))
    builder.Configuration["JwtSettings:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER");
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_AUDIENCE")))
    builder.Configuration["JwtSettings:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

// Override MinIO settings from environment variables
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MINIO_ENDPOINT")))
    builder.Configuration["MinIO:Endpoint"] = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY")))
    builder.Configuration["MinIO:AccessKey"] = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MINIO_SECRET_KEY")))
    builder.Configuration["MinIO:SecretKey"] = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MINIO_BUCKET_NAME")))
    builder.Configuration["MinIO:BucketName"] = Environment.GetEnvironmentVariable("MINIO_BUCKET_NAME");
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MINIO_USE_SSL")))
    builder.Configuration["MinIO:UseSSL"] = Environment.GetEnvironmentVariable("MINIO_USE_SSL");

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
builder.Services.AddScoped<HealthSync.Application.Features.Auth.Interfaces.IAuthService, HealthSync.Application.Features.Auth.Services.AuthService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IUserProfileService, HealthSync.Application.Features.Users.Services.UserProfileService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IUserService, HealthSync.Application.Services.UserService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IUserRepository, HealthSync.Infrastructure.Repositories.UserRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IUserProfileRepository, HealthSync.Infrastructure.Repositories.UserProfileRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.ILeaderboardRepository, HealthSync.Infrastructure.Repositories.LeaderboardRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.ILeaderboardService, HealthSync.Application.Services.LeaderboardService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IJwtService, HealthSync.Infrastructure.Services.JwtService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IExerciseService, HealthSync.Application.Services.ExerciseService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IExerciseRepository, HealthSync.Infrastructure.Repositories.ExerciseRepository>();
// Register storage service (MinIO) - implements both IStorageService and IFileStorageService
builder.Services.AddSingleton<HealthSync.Infrastructure.Services.MinioService>();
builder.Services.AddSingleton<HealthSync.Application.Interfaces.IStorageService>(sp => sp.GetRequiredService<HealthSync.Infrastructure.Services.MinioService>());
builder.Services.AddSingleton<HealthSync.Application.Interfaces.IFileStorageService>(sp => sp.GetRequiredService<HealthSync.Infrastructure.Services.MinioService>());
builder.Services.AddScoped<HealthSync.Application.Interfaces.IFoodItemRepository, HealthSync.Infrastructure.Repositories.FoodItemRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IWorkoutLogRepository, HealthSync.Infrastructure.Repositories.WorkoutLogRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IWorkoutLogService, HealthSync.Application.Services.WorkoutLogService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.INutritionLogService, HealthSync.Application.Services.NutritionLogService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.INutritionLogRepository, HealthSync.Infrastructure.Repositories.NutritionLogRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IForumPostRepository, HealthSync.Infrastructure.Repositories.ForumPostRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IForumReplyRepository, HealthSync.Infrastructure.Repositories.ForumReplyRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IForumCategoryRepository, HealthSync.Infrastructure.Repositories.ForumCategoryRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IForumAdminService, HealthSync.Application.Services.ForumAdminService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IChallengeRepository, HealthSync.Infrastructure.Repositories.ChallengeRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IChallengeParticipationRepository, HealthSync.Infrastructure.Repositories.ChallengeParticipationRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.INotificationRepository, HealthSync.Infrastructure.Repositories.NotificationRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IChallengeAdminService, HealthSync.Application.Services.ChallengeAdminService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IChallengeParticipationService, HealthSync.Application.Services.ChallengeParticipationService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IDashboardAdminService, HealthSync.Application.Services.DashboardAdminService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IExerciseSessionRepository, HealthSync.Infrastructure.Repositories.ExerciseSessionRepository>();

// Register background jobs
builder.Services.AddScoped<HealthSync.Application.Interfaces.ILeaderboardUpdateJob, HealthSync.Infrastructure.BackgroundJobs.LeaderboardUpdateJob>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IPointCalculationService, HealthSync.Application.Services.PointCalculationService>();

// Add Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Enable detailed error pages in development
    if (builder.Configuration.GetValue<bool>("DevelopmentSettings:EnableDetailedErrors"))
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
