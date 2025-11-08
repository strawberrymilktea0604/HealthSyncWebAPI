using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using FluentValidation.AspNetCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<HealthSync.Application.Validators.Users.UpdateUserProfileValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<HealthSync.Application.Validators.Exercises.CreateExerciseRequestValidator>();

// Add DbContext (without Identity)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Authentication (JWT only, no Identity)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? "default-secret-key"))
    };
});

// Add Authorization
builder.Services.AddAuthorization();

// Register application services
builder.Services.AddScoped<HealthSync.Application.Features.Auth.Interfaces.IAuthService, HealthSync.Application.Features.Auth.Services.AuthService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IUserProfileService, HealthSync.Application.Features.Users.Services.UserProfileService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IUserRepository, HealthSync.Infrastructure.Repositories.UserRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IUserProfileRepository, HealthSync.Infrastructure.Repositories.UserProfileRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.ILeaderboardRepository, HealthSync.Infrastructure.Repositories.LeaderboardRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IJwtService, HealthSync.Infrastructure.Services.JwtService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IExerciseService, HealthSync.Application.Services.ExerciseService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IExerciseRepository, HealthSync.Infrastructure.Repositories.ExerciseRepository>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IFileStorageService, HealthSync.Infrastructure.Services.FileStorageService>();
builder.Services.AddScoped<HealthSync.Application.Interfaces.IFoodItemRepository, HealthSync.Infrastructure.Repositories.FoodItemRepository>();

// Add Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HealthSync API", Version = "v1" });

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
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add root endpoint
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
