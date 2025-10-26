# HealthSync API - Copilot Instructions

## Project Overview
HealthSync là nền tảng Quản lý Sức khỏe & Luyện tập Cá nhân, giúp người dùng số hóa toàn bộ nhật ký luyện tập và dinh dưỡng trên một nền tảng duy nhất. Hệ thống hỗ trợ 2 vai trò chính:
- **Customer (Người dùng)**: Quản lý hồ sơ, mục tiêu, ghi nhật ký luyện tập/dinh dưỡng, tham gia thử thách
- **Admin (Quản trị viên)**: Quản lý thư viện bài tập/món ăn, người dùng, thử thách, xem dashboard thống kê

Hệ thống được xây dựng theo Clean Architecture với ASP.NET Core, sử dụng SQL Server cho CSDL và MinIO cho lưu trữ file.

## Tech Stack
- **Framework**: .NET 9.0
- **Web Framework**: ASP.NET Core Web API (Minimal APIs hoặc Controllers)
- **Database**: SQL Server via Entity Framework Core 9.0.10
- **Authentication**: JWT Bearer với Refresh Token (Microsoft.AspNetCore.Authentication.JwtBearer 9.0.10)
- **Authorization**: Role-based Access Control (RBAC) - Customer/Admin roles
- **File Storage**: MinIO (cho ảnh đại diện, ảnh bài tập, ảnh món ăn)
- **API Documentation**: OpenAPI/Swagger (Swashbuckle.AspNetCore 9.0.6)
- **Language Features**: C# with nullable reference types và implicit usings enabled

## Architecture & Project Structure

The solution follows **Clean Architecture** with dependency flow from outer to inner layers:

```
HealthSyncWebAPI.sln
├── HealthSync.Domain/          # Core business entities (no dependencies)
├── HealthSync.Application/     # Business logic & use cases (depends on Domain)
├── HealthSync.Infrastructure/  # Data access & external services (depends on Domain & Application)
└── HealthSync.WebApi/          # API endpoints & presentation (depends on Application & Infrastructure)
```

### Layer Responsibilities

**HealthSync.Domain** (Core Business Entities):
- Entities: `ApplicationUser`, `UserProfile`, `Goal`, `ProgressRecord`, `WorkoutLog`, `ExerciseSession`, `Exercise`, `NutritionLog`, `FoodEntry`, `FoodItem`, `Challenge`, `ChallengeParticipation`
- Value Objects: các đối tượng immutable như `MealType`, `GoalType`, `ChallengeStatus`
- Domain Interfaces: KHÔNG chứa repository interfaces (đặt ở Application layer)
- NO external dependencies

**HealthSync.Application** (Business Logic & Use Cases):
- Interfaces: `IUserRepository`, `IWorkoutRepository`, `INutritionRepository`, `IChallengeRepository`, etc.
- DTOs: Request/Response models cho từng feature
- Services: Business logic như tính calo, validate mục tiêu, tính tiến độ
- Validators: FluentValidation cho input validation
- CQRS Pattern (tùy chọn): Commands/Queries cho từng use case
- References: Domain only

**HealthSync.Infrastructure** (Data Access & External Services):
- DbContext: `ApplicationDbContext` với Entity Configurations
- Repositories: Implement các interfaces từ Application layer
- External Services: MinIO service cho file storage
- Migrations: EF Core migrations
- References: Domain và Application

**HealthSync.WebApi** (API Endpoints & Presentation):
- Controllers hoặc Minimal API endpoints
- Middleware: Authentication, Authorization, Exception handling
- Filters: Action filters, Authorization filters
- DI Configuration: `Program.cs` hoặc extension methods
- References: Application và Infrastructure

## Development Guidelines

### Environment & Shell

- **Operating System**: Windows with PowerShell
- **Shell Commands**: Use PowerShell syntax (`;` for command chaining)
- **Running Processes**: Stop any running instances before building to avoid file locks
  - Check for running processes: `Get-Process | Where-Object {$_.ProcessName -like "*HealthSync*"}`
  - Stop if needed before rebuild

### Building & Running

**Build the solution**:
```powershell
dotnet build HealthSyncWebAPI.sln
```

**Run the API** (from HealthSync.WebApi directory):
```powershell
dotnet run --project HealthSync.WebApi
```

**Access endpoints**:
- HTTP: http://localhost:5258
- HTTPS: https://localhost:7144
- Swagger UI: https://localhost:7144/swagger (Development mode only)

### Creating New Features (Feature-Driven Development)

Tổ chức code theo modules chức năng (theo sơ đồ phân rã chức năng):

1. **Module Structure Example** (`Workout Module`):
```
HealthSync.Domain/
  └── Entities/
      ├── WorkoutLog.cs
      ├── ExerciseSession.cs
      └── Exercise.cs

HealthSync.Application/
  └── Features/
      └── Workouts/
          ├── Interfaces/
          │   └── IWorkoutRepository.cs
          ├── DTOs/
          │   ├── WorkoutLogDto.cs
          │   └── CreateWorkoutRequest.cs
          ├── Services/
          │   └── WorkoutService.cs
          └── Validators/
              └── CreateWorkoutValidator.cs

HealthSync.Infrastructure/
  └── Data/
      ├── Configurations/
      │   ├── WorkoutLogConfiguration.cs
      │   └── ExerciseConfiguration.cs
      └── Repositories/
          └── WorkoutRepository.cs

HealthSync.WebApi/
  └── Controllers/ (hoặc Endpoints/)
      └── WorkoutsController.cs
```

2. **Domain Entities** - Quy tắc thiết kế:
   - Sử dụng file-scoped namespaces: `namespace HealthSync.Domain.Entities;`
   - Properties phải có validation attributes nếu cần
   - Quan hệ: sử dụng navigation properties (1-1, 1-N, N-N)
   - Không chứa business logic phức tạp (để ở Services)

3. **Application Layer** - Business Rules:
   - Interfaces repository đặt ở `Application/Features/{Module}/Interfaces/`
   - DTOs phải tách biệt Request/Response (không dùng chung)
   - Services xử lý business logic: tính toán calo, validate mục tiêu, kiểm tra quyền
   - Async/Await: BẮT BUỘC cho tất cả operations có I/O

4. **Infrastructure Layer** - Data Access:
   - Entity Configurations: Fluent API trong `Data/Configurations/`
   - Repository pattern: implement interfaces từ Application
   - DbContext: chỉ có 1 DbContext chính (`ApplicationDbContext`)
   - Migrations: luôn đặt tên rõ ràng: `Add{Entity}Table`, `Update{Entity}{Field}`

5. **WebApi Layer** - Endpoints:
   - Controllers: tổ chức theo modules (`WorkoutsController`, `NutritionController`, etc.)
   - Authorization: sử dụng `[Authorize(Roles = "Customer")]` hoặc `[Authorize(Roles = "Admin")]`
   - Response format: luôn trả về consistent format (success/error)
   - Validation: sử dụng ModelState hoặc FluentValidation

### Code Style & Conventions (SOLID Principles)

- **Naming Conventions**:
  - PascalCase: Classes, Methods, Properties, Public fields
  - camelCase: Parameters, Local variables, Private fields (với prefix `_`)
  - Interfaces: prefix `I` (ví dụ: `IWorkoutRepository`)
  - Async methods: suffix `Async` (ví dụ: `GetAllAsync()`)
  - DTOs: suffix `Dto`, `Request`, `Response` (ví dụ: `WorkoutLogDto`, `CreateWorkoutRequest`)

- **SOLID Principles** (BẮT BUỘC):
  1. **Single Responsibility**: Mỗi class chỉ có 1 lý do để thay đổi
     - Repository chỉ lo data access
     - Service chỉ lo business logic
     - Controller chỉ lo HTTP request/response
  
  2. **Open/Closed**: Mở cho mở rộng, đóng cho sửa đổi
     - Sử dụng interfaces và abstract classes
     - Dependency Injection cho tất cả dependencies
  
  3. **Liskov Substitution**: Derived classes phải thay thế được base classes
     - Không override methods làm thay đổi hành vi cơ bản
  
  4. **Interface Segregation**: Không ép clients implement methods không dùng
     - Tách interfaces nhỏ, chuyên biệt
     - Ví dụ: `IReadRepository<T>` và `IWriteRepository<T>` thay vì `IRepository<T>` quá lớn
  
  5. **Dependency Inversion**: Phụ thuộc vào abstractions, không phụ thuộc vào concrete classes
     - Controllers phụ thuộc vào `IWorkoutService`, không phụ thuộc vào `WorkoutService`
     - Inject dependencies qua constructor

- **Async/Await Pattern** (BẮT BUỘC):
  ```csharp
  // ✅ ĐÚNG
  public async Task<WorkoutLog> GetWorkoutLogAsync(int id)
  {
      return await _context.WorkoutLogs.FindAsync(id);
  }
  
  // ❌ SAI - không dùng .Result hoặc .Wait()
  public WorkoutLog GetWorkoutLog(int id)
  {
      return _context.WorkoutLogs.FindAsync(id).Result; // DEADLOCK risk!
  }
  ```

- **Nullable Reference Types**: Enabled globally
  - Luôn handle null cases: `if (user is null) return NotFound();`
  - Sử dụng `?` cho nullable types: `string? optionalField`
  
- **File-scoped Namespaces** (C# 10+):
  ```csharp
  namespace HealthSync.Domain.Entities;
  
  public class WorkoutLog
  {
      // ...
  }
  ```

- **Entity Relationships**:
  ```csharp
  public class WorkoutLog
  {
      public int WorkoutLogId { get; set; }
      public int UserId { get; set; }
      public ApplicationUser User { get; set; } = null!; // Navigation property
      public ICollection<ExerciseSession> ExerciseSessions { get; set; } = new List<ExerciseSession>();
  }
  ```

### Database Migrations

Always use the Infrastructure project for migrations:

```powershell
# Add migration
dotnet ef migrations add <MigrationName> --project HealthSync.Infrastructure --startup-project HealthSync.WebApi

# Update database
dotnet ef database update --project HealthSync.Infrastructure --startup-project HealthSync.WebApi

# Remove last migration
dotnet ef migrations remove --project HealthSync.Infrastructure --startup-project HealthSync.WebApi
```

### Configuration Management

- **Connection Strings**:
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HealthSyncDb;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
  }
  ```

- **JWT Settings**:
  ```json
  {
    "JwtSettings": {
      "SecretKey": "your-secret-key-min-32-characters",
      "Issuer": "HealthSyncAPI",
      "Audience": "HealthSyncClient",
      "AccessTokenExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7
    }
  }
  ```

- **MinIO Settings**:
  ```json
  {
    "MinIO": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "BucketName": "healthsync-images",
      "UseSSL": false
    }
  }
  ```

- **Sensitive Data**: 
  - KHÔNG commit `appsettings.Development.json`, `appsettings.Production.json` (đã có trong `.gitignore`)
  - Dùng User Secrets cho development: `dotnet user-secrets set "JwtSettings:SecretKey" "your-secret"`
  - Dùng Environment Variables cho production

### API Development Patterns

**Authentication & Authorization Setup**:
```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
        };
    });

builder.Services.AddAuthorization();

// Middleware order (QUAN TRỌNG!)
app.UseAuthentication();
app.UseAuthorization();
```

**Controller Pattern với Authorization**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class WorkoutsController : ControllerBase
{
    private readonly IWorkoutService _workoutService;
    
    public WorkoutsController(IWorkoutService workoutService)
    {
        _workoutService = workoutService;
    }
    
    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<IEnumerable<WorkoutLogDto>>> GetMyWorkouts()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var workouts = await _workoutService.GetUserWorkoutsAsync(userId);
        return Ok(workouts);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreateExercise([FromBody] CreateExerciseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
            
        var result = await _workoutService.CreateExerciseAsync(request);
        return CreatedAtAction(nameof(GetExercise), new { id = result.Id }, result);
    }
}
```

**Consistent Response Format**:
```csharp
// Success response
return Ok(new { success = true, data = result });

// Error response
return BadRequest(new { success = false, message = "Error details" });

// Not Found
return NotFound(new { success = false, message = "Resource not found" });

// Unauthorized
return Unauthorized(new { success = false, message = "Invalid credentials" });
```

**File Upload Pattern (MinIO)**:
```csharp
[HttpPost("upload-avatar")]
[Authorize(Roles = "Customer")]
public async Task<ActionResult> UploadAvatar(IFormFile file)
{
    if (file == null || file.Length == 0)
        return BadRequest("No file uploaded");
        
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
    var imageUrl = await _minioService.UploadFileAsync(file, "avatars", userId);
    
    await _userService.UpdateAvatarAsync(userId, imageUrl);
    return Ok(new { avatarUrl = imageUrl });
}
```

**Swagger Configuration**:
```csharp
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
```

### Testing Strategy

- Create test projects following naming: `HealthSync.[Layer].Tests`
- Use xUnit, NUnit, or MSTest as testing framework
- Place test projects at solution root level
- Mock external dependencies in unit tests

## Common Pitfalls & Tips

1. **Project References**: Always reference projects in the correct dependency order:
   - WebApi → Application, Infrastructure
   - Infrastructure → Domain, Application
   - Application → Domain
   - Domain → (no dependencies)

2. **EF Core Tools**: Ensure you specify both `--project` and `--startup-project` for migrations

3. **Build Errors**: Run `dotnet restore` if you encounter package-related build errors

4. **HTTPS Development**: Trust the dev certificate: `dotnet dev-certs https --trust`

5. **Port Conflicts**: Default ports are 5258 (HTTP) and 7144 (HTTPS) - modify in `launchSettings.json` if needed

## Package Management

When adding NuGet packages:
- Add **domain-agnostic** packages to respective layers
- Add **EF Core** packages to Infrastructure layer only
- Add **ASP.NET Core** packages to WebApi layer only
- Keep Domain layer free of external dependencies when possible

## Core Domain Entities (ERD Implementation)

### User Management
- `ApplicationUser`: user_id, email, password_hash, role (Customer/Admin), is_active, created_at
- `UserProfile`: user_profile_id, user_id (FK), full_name, gender, dob, height, weight, activity_level, avatar_url
- `Goal`: goal_id, user_id (FK), goal_type, target_value, start_date, end_date, status
- `ProgressRecord`: progress_record_id, goal_id (FK), record_date, record_value, weight, waist_cm

### Workout Module
- `Exercise`: exercise_id, name, muscle_group, difficulty, equipment, description, image_url
- `WorkoutLog`: workout_log_id, user_id (FK), workout_date, duration_min, notes
- `ExerciseSession`: exercise_session_id, workout_log_id (FK), exercise_id (FK), sets, reps, weight_kg, rest_sec, rpe

### Nutrition Module
- `FoodItem`: food_item_id, name, serving_size, serving_unit, calories_kcal, protein_g, carbs_g, fat_g, image_url
- `NutritionLog`: nutrition_log_id, user_id (FK), log_date, total_calories
- `FoodEntry`: food_entry_id, nutrition_log_id (FK), food_item_id (FK), quantity, meal_type, calories_kcal, protein_g, carbs_g, fat_g

### Challenge Module (Nghiệp vụ có trạng thái)
- `Challenge`: challenge_id, title, description, start_date, end_date, status (Open/Closed)
- `ChallengeParticipation`: participation_id, challenge_id (FK), user_id (FK), joined_date, status (Pending/Completed/Failed), submission_url, reviewed_by, review_date

## Business Rules & Workflows

### 1. Quy trình Quản lý Thử thách (Challenge Workflow)
```
1. Admin tạo Challenge → status = 'Open'
2. Customer tham gia → tạo ChallengeParticipation (status = 'Joined')
3. Customer nộp kết quả → status = 'Pending'
4. Admin duyệt → status = 'Completed' hoặc 'Failed'
5. System gửi thông báo cho Customer
```

### 2. Quy trình Ghi nhật ký Luyện tập
```
1. Customer chọn ngày tập
2. Customer chọn bài tập từ thư viện Exercise (include)
3. Customer nhập sets, reps, weight cho mỗi ExerciseSession
4. System tạo WorkoutLog và các ExerciseSession liên quan
5. System tính tổng duration và calories burned
```

### 3. Quy trình Ghi nhật ký Dinh dưỡng
```
1. Customer chọn ngày và bữa ăn (Breakfast/Lunch/Dinner/Snack)
2. Customer tìm và chọn món ăn từ FoodItem
3. Customer nhập quantity
4. System tự động tính calories, protein, carbs, fat dựa trên serving_size
5. System cập nhật total_calories của NutritionLog
```

### 4. Dashboard Thống kê (Admin)
```sql
-- 3 chỉ số chính:
- Tổng số người dùng: COUNT(ApplicationUser)
- Số người dùng mới (tháng này): COUNT(WHERE created_at >= START_OF_MONTH)
- Số nhật ký luyện tập (hôm nay): COUNT(WorkoutLog WHERE workout_date = TODAY)

-- Top 5 bài tập:
SELECT TOP 5 e.name, COUNT(es.exercise_id) as usage_count
FROM Exercise e
JOIN ExerciseSession es ON e.exercise_id = es.exercise_id
GROUP BY e.exercise_id, e.name
ORDER BY usage_count DESC
```

## Development Roadmap

### Phase 1: Foundation (Hiện tại)
- ✅ Solution structure với 4 projects
- ✅ JWT Authentication package installed
- ✅ EF Core với SQL Server configured
- ✅ Swagger/OpenAPI setup
- ⚠️ Remove placeholder `Class1.cs` files

### Phase 2: Core Features
1. **User Management Module**
   - Implement ApplicationUser, UserProfile entities
   - Auth endpoints: Register, Login, Refresh Token
   - Profile management: Update profile, upload avatar (MinIO)

2. **Workout Module**
   - Implement Exercise, WorkoutLog, ExerciseSession
   - Admin: CRUD thư viện bài tập
   - Customer: Ghi nhật ký luyện tập, xem lịch sử

3. **Nutrition Module**
   - Implement FoodItem, NutritionLog, FoodEntry
   - Admin: CRUD thư viện món ăn
   - Customer: Ghi nhật ký dinh dưỡng, tự động tính calo

4. **Goal & Progress Module**
   - Implement Goal, ProgressRecord
   - Customer: Thiết lập mục tiêu, ghi tiến độ
   - Customer: Xem biểu đồ tiến độ (line chart)

### Phase 3: Advanced Features
5. **Challenge Module** (Nghiệp vụ tương tác 2 chiều)
   - Implement Challenge, ChallengeParticipation
   - Admin: Tạo thử thách, duyệt kết quả
   - Customer: Tham gia, nộp kết quả

6. **Dashboard & Reporting**
   - Admin: Xem thống kê tổng quan
   - Biểu đồ: Top 5 bài tập, xu hướng người dùng
   - Customer: Báo cáo tiến độ cá nhân

### Phase 4: Infrastructure
7. **MinIO Integration**
   - Upload/Download images cho: Avatar, Exercise, FoodItem
   - Image optimization và validation

8. **CI/CD Pipeline**
   - GitHub → Jenkins → Docker Compose
   - NGINX load balancing (2 API instances)
   - Ngrok cho public access (dev/testing)

## Common Patterns

### Repository Pattern
```csharp
public interface IWorkoutRepository
{
    Task<WorkoutLog?> GetByIdAsync(int id);
    Task<IEnumerable<WorkoutLog>> GetUserWorkoutsAsync(int userId);
    Task<WorkoutLog> AddAsync(WorkoutLog workoutLog);
    Task UpdateAsync(WorkoutLog workoutLog);
    Task DeleteAsync(int id);
}
```

### Service Pattern
```csharp
public class WorkoutService : IWorkoutService
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IExerciseRepository _exerciseRepository;
    
    public async Task<WorkoutLogDto> CreateWorkoutAsync(CreateWorkoutRequest request, int userId)
    {
        // 1. Validate request
        // 2. Map to entity
        // 3. Calculate calories
        // 4. Save to repository
        // 5. Return DTO
    }
}
```
