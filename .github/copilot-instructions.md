# HealthSync API - Copilot Instructions

## Project Overview
HealthSync là nền tảng số hướng tới người dùng cá nhân (sinh viên, nhân viên văn phòng) có nhu cầu quản lý luyện tập, dinh dưỡng và mục tiêu sức khỏe. Trong bối cảnh dữ liệu sức khỏe còn rời rạc, phân mảnh, hệ thống đặt mục tiêu hợp nhất nhật ký luyện tập và dinh dưỡng, cung cấp công cụ theo dõi tiến độ và tương tác cộng đồng, đồng thời hỗ trợ quản trị nội dung và báo cáo thống kê cho quản trị viên.

### Core Objectives
1. **Số hóa và tập trung hóa**: Nhật ký luyện tập, dinh dưỡng, mục tiêu và tiến độ cá nhân
2. **Tự động hóa tính toán**: Năng lượng vào/ra (calories) và các chỉ số dinh dưỡng đa lượng (macros)
3. **Quản trị nội dung**: Bài tập, món ăn, thử thách cộng đồng và báo cáo thống kê hành vi người dùng
4. **Kiến trúc sạch**: Dễ mở rộng, đáp ứng các nguyên tắc SOLID và Clean Architecture

### User Roles & Scope
- **Customer (Người dùng cuối)**: Quản lý hồ sơ, mục tiêu, ghi nhật ký luyện tập/dinh dưỡng, theo dõi tiến độ, tham gia thử thách cộng đồng
- **Admin (Quản trị viên)**: Quản lý người dùng, thư viện bài tập và món ăn, thiết lập thử thách cộng đồng, xem báo cáo và dashboard thống kê

Hệ thống được xây dựng theo Clean Architecture với ASP.NET Core, sử dụng SQL Server cho CSDL và MinIO/Object Storage cho lưu trữ file.

## Tech Stack
- **Framework**: .NET 9.0
- **Web Framework**: ASP.NET Core Web API (Controllers hoặc Minimal APIs)
- **Database**: SQL Server via Entity Framework Core 9.0.10
- **Authentication**: JWT Bearer với Refresh Token (Microsoft.AspNetCore.Authentication.JwtBearer 9.0.10)
- **OAuth2 Social Login**: Hỗ trợ Google/Facebook (ánh xạ về ApplicationUser)
- **Authorization**: Role-based Access Control (RBAC) - Customer/Admin roles
- **File Storage**: MinIO hoặc Object Storage (ảnh đại diện, ảnh bài tập, ảnh món ăn, minh chứng thử thách)
- **API Documentation**: OpenAPI/Swagger (Swashbuckle.AspNetCore 9.0.6)
- **Validation**: FluentValidation hoặc Data Annotations
- **Language Features**: C# with nullable reference types và implicit usings enabled
- **Logging**: Structured logging (Serilog khuyến nghị)
- **Caching**: In-memory hoặc Redis (tùy chọn)
- **Background Jobs**: Hangfire hoặc Quartz.NET cho tác vụ tổng hợp định kỳ

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
- Entities: `ApplicationUser`, `UserProfile`, `Goal`, `ProgressRecord`, `WorkoutLog`, `ExerciseSession`, `Exercise`, `NutritionLog`, `FoodEntry`, `FoodItem`, `Challenge`, `ChallengeParticipation`, `UserChallengeSubmission`
- Value Objects: các đối tượng immutable như `MealType`, `GoalType`, `ChallengeStatus`, `ActivityLevel`, `DifficultyLevel`, `MuscleGroup`
- Business Rules: Domain validation logic (ví dụ: validate phạm vi cân nặng hợp lệ, kiểm tra ngày bắt đầu < ngày kết thúc mục tiêu)
- Domain Interfaces: KHÔNG chứa repository interfaces (đặt ở Application layer)
- NO external dependencies - giữ Domain hoàn toàn thuần túy

**HealthSync.Application** (Business Logic & Use Cases):
- Interfaces: `IUserRepository`, `IWorkoutRepository`, `INutritionRepository`, `IChallengeRepository`, `IFileStorageService`, `IEmailService`, etc.
- DTOs: Request/Response models cho từng feature module
- Services: Business logic như:
  - Tính toán calories tiêu thụ/nạp vào
  - Tính toán macros (protein, carbs, fat)
  - Validate mục tiêu (phạm vi hợp lý, thời hạn)
  - Tính tiến độ hoàn thành mục tiêu (%)
  - Quy trình duyệt kết quả thử thách (state transitions)
- Validators: FluentValidation cho input validation
- CQRS Pattern (tùy chọn): Commands/Queries cho từng use case
- Use Cases: Tổ chức theo modules (User, Workout, Nutrition, Goal, Challenge, Admin)
- References: Domain only

**HealthSync.Infrastructure** (Data Access & External Services):
- DbContext: `ApplicationDbContext` với Entity Configurations (Fluent API)
- Repositories: Implement các interfaces từ Application layer
- External Services:
  - MinIO/Object Storage service cho file upload/download
  - Email service cho notifications (thông báo duyệt thử thách)
  - OAuth2 providers (Google, Facebook)
- Migrations: EF Core migrations với tên rõ ràng
- Background Jobs: Hangfire/Quartz jobs cho tác vụ tổng hợp (Dashboard stats)
- References: Domain và Application

**HealthSync.WebApi** (API Endpoints & Presentation):
- Controllers hoặc Minimal API endpoints tổ chức theo modules:
  - `AuthController`: Register, Login, Refresh Token, OAuth2 callback
  - `UsersController`: Profile management, avatar upload
  - `GoalsController`: CRUD goals, progress records
  - `WorkoutsController`: Workout logs, exercise sessions
  - `NutritionController`: Nutrition logs, food entries
  - `ChallengesController`: Join, submit, view challenges
  - `AdminController`: Dashboard stats, user management
  - `ExercisesController` (Admin): CRUD exercise library
  - `FoodItemsController` (Admin): CRUD food library
- Middleware: Authentication, Authorization, Exception handling, Request logging
- Filters: Action filters, Authorization filters, Model validation
- DI Configuration: `Program.cs` hoặc extension methods (ServiceCollectionExtensions)
- API Versioning: `/api/v1/...` pattern
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

### User Management Module
```csharp
// ApplicationUser: Tài khoản xác thực
- user_id (PK)
- email (unique, indexed)
- password_hash (bcrypt/PBKDF2)
- role (Customer | Admin)
- is_active (bool, default: true)
- email_confirmed (bool)
- oauth_provider (null | Google | Facebook)
- oauth_provider_id (nullable)
- created_at
- last_login_at

// UserProfile: Thông tin sinh trắc học (1-1 với ApplicationUser)
- user_profile_id (PK)
- user_id (FK unique)
- full_name
- date_of_birth
- gender (Male | Female | Other)
- height_cm (decimal)
- current_weight_kg (decimal)
- activity_level (Sedentary | LightlyActive | ModeratelyActive | VeryActive | ExtraActive)
- avatar_url (nullable, MinIO path)
- created_at
- updated_at
```

### Goal & Progress Module
```csharp
// Goal: Mục tiêu cá nhân
- goal_id (PK)
- user_id (FK, indexed)
- goal_type (WeightLoss | WeightGain | MaintainWeight | BodyMeasurement)
- target_value (decimal, ví dụ: 65.0 kg hoặc 80.0 cm vòng eo)
- unit (kg | cm | %)
- start_date
- end_date
- status (InProgress | Completed | Cancelled)
- created_at

// ProgressRecord: Ghi lại tiến độ theo thời gian
- progress_record_id (PK)
- goal_id (FK, indexed)
- record_date
- recorded_value (decimal, giá trị đo được)
- weight_kg (decimal, nullable, ghi lại cân nặng tại thời điểm)
- waist_cm (decimal, nullable)
- chest_cm (decimal, nullable)
- hip_cm (decimal, nullable)
- notes (text, nullable)
- created_at
```

### Workout Module
```csharp
// Exercise: Thư viện bài tập (quản lý bởi Admin)
- exercise_id (PK)
- name (indexed)
- muscle_group (Chest | Back | Legs | Shoulders | Arms | Core | Cardio | FullBody)
- difficulty_level (Beginner | Intermediate | Advanced)
- equipment (Barbell | Dumbbell | Machine | Bodyweight | Cable | Kettlebell | Resistance Band | Other)
- description (text)
- instructions (text, nullable)
- image_url (nullable, MinIO path)
- video_url (nullable)
- calories_per_minute (decimal, nullable, ước tính)
- created_by_admin_id (FK)
- created_at
- updated_at

// WorkoutLog: Nhật ký buổi tập
- workout_log_id (PK)
- user_id (FK, indexed)
- workout_date (indexed)
- total_duration_minutes (int, tổng thời gian tập)
- estimated_calories_burned (decimal, tự động tính)
- notes (text, nullable)
- created_at

// ExerciseSession: Chi tiết bài tập trong buổi tập (N-N giữa WorkoutLog và Exercise)
- exercise_session_id (PK)
- workout_log_id (FK, indexed)
- exercise_id (FK, indexed)
- sets (int)
- reps (int, reps mỗi set - có thể trung bình)
- weight_kg (decimal, nullable)
- rest_seconds (int, nullable)
- rpe (Rate of Perceived Exertion, 1-10, nullable)
- duration_minutes (int, nullable, cho cardio)
- notes (text, nullable)
- order_index (int, thứ tự trong buổi tập)
```

### Nutrition Module
```csharp
// FoodItem: Thư viện món ăn (quản lý bởi Admin)
- food_item_id (PK)
- name (indexed)
- category (Protein | Carbs | Fats | Vegetables | Fruits | Dairy | Beverages | Snacks | Other)
- serving_size (decimal, ví dụ: 100)
- serving_unit (g | ml | piece | cup | tbsp)
- calories_per_serving (decimal)
- protein_g (decimal)
- carbs_g (decimal)
- fat_g (decimal)
- fiber_g (decimal, nullable)
- sugar_g (decimal, nullable)
- description (text, nullable)
- image_url (nullable, MinIO path)
- created_by_admin_id (FK)
- created_at
- updated_at

// NutritionLog: Nhật ký dinh dưỡng theo ngày
- nutrition_log_id (PK)
- user_id (FK, indexed)
- log_date (indexed)
- total_calories (decimal, tự động tính từ FoodEntry)
- total_protein_g (decimal, tự động tính)
- total_carbs_g (decimal, tự động tính)
- total_fat_g (decimal, tự động tính)
- notes (text, nullable)
- created_at

// FoodEntry: Chi tiết món ăn trong ngày (N-N giữa NutritionLog và FoodItem)
- food_entry_id (PK)
- nutrition_log_id (FK, indexed)
- food_item_id (FK, indexed)
- meal_type (Breakfast | Lunch | Dinner | Snack)
- quantity (decimal, số lượng khẩu phần, ví dụ: 1.5)
- calories (decimal, = quantity × calories_per_serving)
- protein_g (decimal, tự động tính)
- carbs_g (decimal, tự động tính)
- fat_g (decimal, tự động tính)
- consumed_at (datetime, nullable, thời điểm ăn chính xác)
- notes (text, nullable)
```

### Challenge Module (Nghiệp vụ có trạng thái)
```csharp
// Challenge: Thử thách cộng đồng (do Admin tạo)
- challenge_id (PK)
- title
- description (text)
- challenge_type (Workout | Nutrition | Hybrid)
- start_date
- end_date
- criteria (text, tiêu chí hoàn thành, ví dụ: "Chạy 50km trong 30 ngày")
- status (Open | Closed)
- max_participants (int, nullable)
- reward_description (text, nullable)
- image_url (nullable, MinIO path)
- created_by_admin_id (FK)
- created_at
- updated_at

// ChallengeParticipation / UserChallengeSubmission: Trạng thái tham gia
- participation_id (PK)
- challenge_id (FK, indexed)
- user_id (FK, indexed)
- joined_date
- status (Joined | PendingApproval | Completed | Failed)
- submission_text (text, nullable, mô tả kết quả)
- submission_url (nullable, MinIO path, ảnh/video minh chứng)
- submitted_at (datetime, nullable)
- reviewed_by_admin_id (FK, nullable)
- review_date (datetime, nullable)
- review_notes (text, nullable)
- completed_at (datetime, nullable)
- created_at
```

### Relationships Summary (ERD)
```
ApplicationUser 1---1 UserProfile
ApplicationUser 1---N Goal
ApplicationUser 1---N WorkoutLog
ApplicationUser 1---N NutritionLog
ApplicationUser 1---N ChallengeParticipation

Goal 1---N ProgressRecord

Exercise 1---N ExerciseSession
WorkoutLog 1---N ExerciseSession

FoodItem 1---N FoodEntry
NutritionLog 1---N FoodEntry

Challenge 1---N ChallengeParticipation
ApplicationUser (Admin) 1---N Exercise (created_by)
ApplicationUser (Admin) 1---N FoodItem (created_by)
ApplicationUser (Admin) 1---N Challenge (created_by)
ApplicationUser (Admin) 1---N ChallengeParticipation (reviewed_by)
```

## Business Rules & Workflows

### 1. Quy trình Quản lý Thử thách (Challenge Workflow với State Machine)
```
Trạng thái Challenge:
- Open: Admin tạo mới, người dùng có thể tham gia
- Closed: Đã đóng, không nhận thêm tham gia

Trạng thái ChallengeParticipation:
1. Joined: Customer vừa tham gia
2. PendingApproval: Customer đã nộp kết quả, chờ Admin duyệt
3. Completed: Admin duyệt thành công
4. Failed: Admin không duyệt hoặc không hoàn thành

Workflow:
1. Admin tạo Challenge → status = 'Open'
2. Customer join Challenge → tạo ChallengeParticipation (status = 'Joined')
3. Customer nộp kết quả (submission_url, submission_text) → status = 'PendingApproval'
4. Admin review:
   - Approve → status = 'Completed', ghi review_notes, set completed_at
   - Reject → status = 'Failed', ghi review_notes
5. System gửi notification cho Customer (Email/In-app)
6. Admin đóng Challenge → status = 'Closed' (không cho join thêm)

Business Rules:
- Không thể join Challenge đã Closed
- Không thể nộp kết quả nếu chưa join
- Chỉ có thể nộp 1 lần (hoặc cho phép nộp lại nếu Failed)
- Admin không thể tự join Challenge của mình (tùy chọn)
```

### 2. Quy trình Ghi nhật ký Luyện tập (Workout Logging)
```
1. Customer chọn workout_date (mặc định: hôm nay)
2. Tạo WorkoutLog mới với user_id, workout_date
3. Thêm các ExerciseSession:
   a. Customer tìm và chọn Exercise từ thư viện (search by name, muscle_group, equipment)
   b. Customer nhập: sets, reps, weight_kg, rest_seconds, RPE, order_index
   c. System validate: sets > 0, reps > 0, weight >= 0
   d. Lưu ExerciseSession với workout_log_id và exercise_id
4. System tính toán:
   - total_duration_minutes = SUM(ExerciseSession.duration_minutes hoặc ước tính từ sets × reps × rest_seconds)
   - estimated_calories_burned = SUM(Exercise.calories_per_minute × duration) (nếu có)
5. Customer có thể thêm notes cho WorkoutLog
6. Lưu WorkoutLog với thông tin tổng hợp

Business Rules:
- Một WorkoutLog có thể chứa nhiều ExerciseSession (1 buổi tập nhiều bài)
- Một Exercise có thể xuất hiện nhiều lần trong cùng WorkoutLog (ví dụ: superset)
- Không cho phép workout_date trong tương lai (hoặc chỉ cảnh báo)
- RPE (Rate of Perceived Exertion): scale 1-10
```

### 3. Quy trình Ghi nhật ký Dinh dưỡng (Nutrition Logging với Auto-calculation)
```
1. Customer chọn log_date (mặc định: hôm nay)
2. System tìm hoặc tạo NutritionLog cho user_id + log_date (unique constraint)
3. Thêm FoodEntry:
   a. Customer chọn meal_type (Breakfast | Lunch | Dinner | Snack)
   b. Customer tìm FoodItem từ thư viện (search by name, category)
   c. Customer nhập quantity (số khẩu phần, ví dụ: 1.5 = 1.5 serving)
   d. System tự động tính:
      - calories = quantity × FoodItem.calories_per_serving
      - protein_g = quantity × FoodItem.protein_g
      - carbs_g = quantity × FoodItem.carbs_g
      - fat_g = quantity × FoodItem.fat_g
   e. Lưu FoodEntry với nutrition_log_id và food_item_id
4. System cập nhật NutritionLog:
   - total_calories = SUM(FoodEntry.calories)
   - total_protein_g = SUM(FoodEntry.protein_g)
   - total_carbs_g = SUM(FoodEntry.carbs_g)
   - total_fat_g = SUM(FoodEntry.fat_g)
5. Hiển thị tổng kết cho Customer

Business Rules:
- Một NutritionLog chứa nhiều FoodEntry (nhiều món ăn trong ngày)
- Một FoodItem có thể xuất hiện nhiều lần (ví dụ: ăn cơm cả sáng lẫn trưa)
- Validate: quantity > 0
- Tự động tính toán, Customer KHÔNG nhập trực tiếp calories/macros
- Cho phép chỉnh sửa FoodEntry → tự động cập nhật lại NutritionLog totals
```

### 4. Quy trình Quản lý Mục tiêu và Tiến độ (Goal & Progress Tracking)
```
1. Customer tạo Goal:
   - Chọn goal_type (WeightLoss | WeightGain | MaintainWeight | BodyMeasurement)
   - Nhập target_value (ví dụ: 65 kg hoặc 80 cm vòng eo)
   - Chọn start_date, end_date (end_date > start_date)
   - Status mặc định: InProgress
2. Customer ghi ProgressRecord định kỳ:
   - Chọn goal_id
   - Nhập record_date (thường là hôm nay)
   - Nhập recorded_value (giá trị đo được)
   - Tùy chọn: weight_kg, waist_cm, chest_cm, hip_cm (cho detailed tracking)
   - Thêm notes nếu cần
3. System tính toán tiến độ:
   - Lấy giá trị ban đầu từ ProgressRecord đầu tiên (hoặc UserProfile.current_weight_kg)
   - Tính % hoàn thành:
     * progress_percent = (current_value - initial_value) / (target_value - initial_value) × 100
   - Hiển thị biểu đồ line chart: record_date vs recorded_value
4. Khi hoàn thành:
   - Customer hoặc System tự động đánh dấu Goal.status = Completed
   - Điều kiện: recorded_value >= target_value (hoặc <= tùy goal_type)

Business Rules:
- Một User có thể có nhiều Goal đồng thời (ví dụ: giảm cân + tăng cơ ngực)
- ProgressRecord phải có record_date nằm trong khoảng [start_date, end_date] của Goal
- Không cho phép duplicate ProgressRecord (cùng goal_id + record_date)
- Khi tạo Goal mới, tự động tạo ProgressRecord đầu tiên với giá trị ban đầu từ UserProfile
```

### 5. Dashboard Thống kê (Admin Analytics & Reporting)
```sql
-- Dashboard Queries:

-- 1. Tổng số người dùng:
SELECT COUNT(*) as TotalUsers 
FROM ApplicationUser 
WHERE is_active = 1;

-- 2. Số người dùng mới trong tháng này:
SELECT COUNT(*) as NewUsersThisMonth
FROM ApplicationUser
WHERE created_at >= DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE()), 0);

-- 3. Số nhật ký luyện tập hôm nay:
SELECT COUNT(*) as WorkoutLogsToday
FROM WorkoutLog
WHERE CAST(workout_date AS DATE) = CAST(GETDATE() AS DATE);

-- 4. Top 5 bài tập được sử dụng nhiều nhất:
SELECT TOP 5 
    e.name, 
    e.muscle_group,
    COUNT(es.exercise_session_id) as UsageCount
FROM Exercise e
INNER JOIN ExerciseSession es ON e.exercise_id = es.exercise_id
GROUP BY e.exercise_id, e.name, e.muscle_group
ORDER BY UsageCount DESC;

-- 5. Top 5 món ăn được ghi nhận nhiều nhất:
SELECT TOP 5 
    f.name, 
    f.category,
    COUNT(fe.food_entry_id) as UsageCount
FROM FoodItem f
INNER JOIN FoodEntry fe ON f.food_item_id = fe.food_item_id
GROUP BY f.food_item_id, f.name, f.category
ORDER BY UsageCount DESC;

-- 6. Xu hướng người dùng mới theo tháng (6 tháng gần nhất):
SELECT 
    YEAR(created_at) as Year,
    MONTH(created_at) as Month,
    COUNT(*) as NewUsers
FROM ApplicationUser
WHERE created_at >= DATEADD(MONTH, -6, GETDATE())
GROUP BY YEAR(created_at), MONTH(created_at)
ORDER BY Year, Month;

-- 7. Thống kê thử thách:
SELECT 
    status,
    COUNT(*) as ChallengeCount
FROM Challenge
GROUP BY status;

-- 8. Tỷ lệ hoàn thành thử thách:
SELECT 
    c.title,
    COUNT(CASE WHEN cp.status = 'Completed' THEN 1 END) as CompletedCount,
    COUNT(CASE WHEN cp.status = 'Failed' THEN 1 END) as FailedCount,
    COUNT(*) as TotalParticipants,
    CAST(COUNT(CASE WHEN cp.status = 'Completed' THEN 1 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) as CompletionRate
FROM Challenge c
LEFT JOIN ChallengeParticipation cp ON c.challenge_id = cp.challenge_id
GROUP BY c.challenge_id, c.title;
```

### 6. Tác vụ tổng hợp định kỳ (Background Jobs)
```
Sử dụng Hangfire hoặc Quartz.NET:

1. Daily Job (chạy lúc 00:05 mỗi ngày):
   - Tổng hợp thống kê ngày hôm qua → lưu vào bảng DailyStats (tùy chọn)
   - Kiểm tra Goal hết hạn → tự động đánh dấu status = Cancelled nếu chưa hoàn thành
   - Kiểm tra Challenge hết hạn → tự động đóng (status = Closed)

2. Hourly Job:
   - Cache Dashboard stats vào Redis/Memory để giảm load database

3. Weekly Job (chạy Chủ nhật):
   - Gửi email báo cáo tiến độ cho Customer (tùy chọn)
   - Cleanup ảnh không sử dụng trên MinIO (orphan files)

4. On-demand Job:
   - Export data (CSV/Excel) cho Admin
   - Generate monthly reports
```

## Use Cases (Functional Requirements)

### Customer Use Cases

#### UC-01: Đăng ký và Đăng nhập
**Actors**: Guest, Customer
**Preconditions**: Không có
**Main Flow**:
1. User truy cập `/api/v1/auth/register`
2. User nhập: email, password, full_name
3. System validate:
   - Email chưa tồn tại
   - Password đủ mạnh (min 8 ký tự, có chữ hoa, số, ký tự đặc biệt)
4. System hash password (bcrypt), tạo ApplicationUser (role = Customer, is_active = true)
5. System tự động tạo UserProfile (1-1 relationship)
6. System trả về success message
7. User login tại `/api/v1/auth/login` với email + password
8. System xác thực, generate JWT access token (15 min) + refresh token (7 days)
9. System trả về tokens và user info (id, email, role, full_name)

**Alternative Flows**:
- 3a. Email đã tồn tại → return 400 "Email already registered"
- 3b. Password yếu → return 400 "Password does not meet requirements"
- 8a. OAuth2 Login (Google/Facebook):
  - User click "Login with Google"
  - System redirect to Google OAuth consent screen
  - User authorize → Google callback với authorization code
  - System exchange code for user info (email, name, profile_picture)
  - System tìm hoặc tạo ApplicationUser với oauth_provider = 'Google'
  - System generate JWT tokens

**Postconditions**: User có access token để gọi protected APIs

#### UC-02: Quản lý Hồ sơ cá nhân
**Actors**: Customer
**Preconditions**: User đã đăng nhập
**Main Flow**:
1. User GET `/api/v1/users/profile` để xem thông tin hiện tại
2. System trả về UserProfile (full_name, dob, gender, height_cm, current_weight_kg, activity_level, avatar_url)
3. User cập nhật thông tin qua PUT `/api/v1/users/profile`
4. System validate:
   - height_cm > 0 và < 300
   - current_weight_kg > 0 và < 500
   - date_of_birth < today
5. System lưu thay đổi, cập nhật updated_at
6. System trả về UserProfile mới

**Alternative Flows**:
- 3a. Upload avatar:
  - User POST `/api/v1/users/avatar` với file (multipart/form-data)
  - System validate: MIME type (image/jpeg, image/png), file size < 5MB
  - System upload to MinIO bucket "avatars"
  - System generate public URL hoặc pre-signed URL
  - System update UserProfile.avatar_url
  - System delete old avatar from MinIO (nếu có)

**Postconditions**: UserProfile được cập nhật thành công

#### UC-03: Ghi nhật ký Luyện tập
**Actors**: Customer
**Preconditions**: User đã đăng nhập, Exercise library đã có dữ liệu
**Main Flow**:
1. User tạo WorkoutLog mới: POST `/api/v1/workouts`
   - Body: { "workout_date": "2025-11-02", "notes": "Chest day" }
2. System tạo WorkoutLog với user_id = current user
3. User thêm ExerciseSession: POST `/api/v1/workouts/{workout_log_id}/exercises`
   - Body: {
       "exercise_id": 5,
       "sets": 4,
       "reps": 10,
       "weight_kg": 60,
       "rest_seconds": 90,
       "rpe": 8,
       "order_index": 1
     }
4. System validate:
   - exercise_id tồn tại trong Exercise table
   - sets > 0, reps > 0, weight_kg >= 0
   - RPE trong khoảng 1-10
5. System lưu ExerciseSession
6. User lặp lại bước 3-5 cho các bài tập khác
7. System tính toán:
   - total_duration_minutes = SUM(estimated duration từ sets/reps/rest)
   - estimated_calories_burned (nếu Exercise có calories_per_minute)
8. System cập nhật WorkoutLog
9. User GET `/api/v1/workouts/{id}` để xem chi tiết buổi tập

**Alternative Flows**:
- 3a. User search Exercise: GET `/api/v1/exercises?search=bench press&muscle_group=Chest`
- 7a. User xóa ExerciseSession: DELETE `/api/v1/workouts/exercises/{session_id}` → recalculate totals

**Postconditions**: WorkoutLog và ExerciseSession được lưu, có thể xem lại lịch sử

#### UC-04: Ghi nhật ký Dinh dưỡng
**Actors**: Customer
**Preconditions**: User đã đăng nhập, FoodItem library đã có dữ liệu
**Main Flow**:
1. User GET `/api/v1/nutrition/daily/2025-11-02` (lấy hoặc tạo NutritionLog cho ngày)
2. System tìm hoặc tạo mới NutritionLog (unique constraint: user_id + log_date)
3. User search món ăn: GET `/api/v1/foods?search=chicken breast&category=Protein`
4. User thêm món ăn: POST `/api/v1/nutrition/daily/2025-11-02/entries`
   - Body: {
       "food_item_id": 12,
       "meal_type": "Lunch",
       "quantity": 1.5
     }
5. System validate:
   - food_item_id tồn tại
   - quantity > 0
6. System tính toán FoodEntry:
   - calories = quantity × FoodItem.calories_per_serving = 1.5 × 165 = 247.5
   - protein_g = 1.5 × 31 = 46.5
   - carbs_g = 1.5 × 0 = 0
   - fat_g = 1.5 × 3.6 = 5.4
7. System lưu FoodEntry
8. System cập nhật NutritionLog totals:
   - total_calories = SUM(FoodEntry.calories)
   - total_protein_g = SUM(FoodEntry.protein_g)
   - total_carbs_g = SUM(FoodEntry.carbs_g)
   - total_fat_g = SUM(FoodEntry.fat_g)
9. System trả về NutritionLog với tất cả FoodEntry và totals

**Alternative Flows**:
- 7a. User sửa FoodEntry: PUT `/api/v1/nutrition/entries/{id}` với quantity mới → recalculate
- 7b. User xóa FoodEntry: DELETE `/api/v1/nutrition/entries/{id}` → recalculate totals

**Postconditions**: NutritionLog được cập nhật với tổng calories/macros chính xác

#### UC-05: Thiết lập Mục tiêu
**Actors**: Customer
**Preconditions**: User đã đăng nhập, UserProfile đã có weight hiện tại
**Main Flow**:
1. User POST `/api/v1/goals` để tạo mục tiêu mới
   - Body: {
       "goal_type": "WeightLoss",
       "target_value": 65,
       "unit": "kg",
       "start_date": "2025-11-01",
       "end_date": "2026-02-01"
     }
2. System validate:
   - end_date > start_date
   - target_value hợp lý (ví dụ: không giảm quá 30% cân nặng hiện tại)
3. System tạo Goal với status = InProgress
4. System tự động tạo ProgressRecord đầu tiên:
   - record_date = start_date
   - recorded_value = UserProfile.current_weight_kg (hoặc user nhập)
   - weight_kg = UserProfile.current_weight_kg
5. System trả về Goal và ProgressRecord ban đầu

**Alternative Flows**:
- 1a. Goal type = BodyMeasurement:
   - target_value = 80 (vòng eo)
   - unit = cm
   - ProgressRecord có thêm waist_cm, chest_cm, hip_cm

**Postconditions**: Goal được tạo với ProgressRecord ban đầu

#### UC-06: Theo dõi Tiến độ
**Actors**: Customer
**Preconditions**: User đã có Goal, đã có ít nhất 1 ProgressRecord
**Main Flow**:
1. User POST `/api/v1/goals/{goal_id}/progress` để ghi tiến độ mới
   - Body: {
       "record_date": "2025-11-15",
       "recorded_value": 67.5,
       "weight_kg": 67.5,
       "notes": "Giảm 2.5kg trong 2 tuần"
     }
2. System validate:
   - goal_id thuộc về current user
   - record_date nằm trong [start_date, end_date]
   - Không duplicate (cùng goal_id + record_date)
3. System lưu ProgressRecord
4. User GET `/api/v1/goals/{goal_id}/chart` để xem biểu đồ
5. System trả về:
   - Goal info (type, target_value, dates)
   - Array of ProgressRecord (record_date, recorded_value)
   - Calculated progress_percent:
     * initial = 70 kg (first ProgressRecord)
     * current = 67.5 kg
     * target = 65 kg
     * progress = (70 - 67.5) / (70 - 65) × 100 = 50%
6. Frontend render line chart: X-axis = record_date, Y-axis = recorded_value

**Alternative Flows**:
- 3a. Goal đã hoàn thành (recorded_value <= target_value):
   - System tự động set Goal.status = Completed
   - Set completed_at = record_date

**Postconditions**: Tiến độ được cập nhật, hiển thị trên biểu đồ

#### UC-07: Tham gia Thử thách
**Actors**: Customer
**Preconditions**: User đã đăng nhập, có Challenge ở trạng thái Open
**Main Flow**:
1. User GET `/api/v1/challenges` để xem danh sách thử thách mở
2. System trả về Challenge với status = Open, end_date >= today
3. User chọn Challenge và POST `/api/v1/challenges/{challenge_id}/join`
4. System validate:
   - Challenge.status = Open
   - User chưa join Challenge này (check ChallengeParticipation)
   - Chưa quá max_participants (nếu có)
5. System tạo ChallengeParticipation:
   - status = Joined
   - joined_date = today
6. System trả về participation info
7. User hoàn thành thử thách, POST `/api/v1/challenges/{challenge_id}/submit`
   - Body: {
       "submission_text": "Đã chạy 50km trong 30 ngày",
       "submission_url": "https://minio.../proof.jpg"
     }
8. System validate: ChallengeParticipation.status = Joined
9. System upload proof image (nếu có file)
10. System cập nhật ChallengeParticipation:
    - status = PendingApproval
    - submission_text, submission_url
    - submitted_at = now
11. System gửi notification cho Admin (email/in-app)

**Alternative Flows**:
- 4a. User đã join rồi → return 400 "Already joined"
- 4b. Challenge closed → return 400 "Challenge is closed"
- 7a. Upload proof: POST multipart/form-data với file → upload to MinIO → get URL

**Postconditions**: ChallengeParticipation ở trạng thái PendingApproval, chờ Admin duyệt

### Admin Use Cases

#### UC-A01: Xem Dashboard Thống kê
**Actors**: Admin
**Preconditions**: Admin đã đăng nhập
**Main Flow**:
1. Admin GET `/api/v1/admin/dashboard/stats`
2. System query database:
   - total_users = COUNT(ApplicationUser WHERE is_active = 1)
   - new_users_this_month = COUNT(WHERE created_at >= start_of_month)
   - workouts_today = COUNT(WorkoutLog WHERE workout_date = today)
3. System trả về stats object
4. Admin GET `/api/v1/admin/dashboard/top-exercises`
5. System query top 5 exercises với usage count
6. Admin GET `/api/v1/admin/dashboard/user-growth`
7. System trả về monthly new users (6 tháng gần nhất)
8. Frontend render charts (line chart cho user growth, bar chart cho top exercises)

**Alternative Flows**:
- 2a. Sử dụng cached data từ Redis/Memory (refresh mỗi giờ)

**Postconditions**: Admin thấy được tổng quan hệ thống

#### UC-A02: Quản lý Người dùng
**Actors**: Admin
**Preconditions**: Admin đã đăng nhập
**Main Flow**:
1. Admin GET `/api/v1/admin/users?page=1&size=20&search=john&role=Customer`
2. System trả về paginated list of users (id, email, full_name, role, is_active, created_at)
3. Admin click vào user để xem chi tiết: GET `/api/v1/admin/users/{user_id}`
4. System trả về full user info + UserProfile + stats (total workouts, total nutrition logs)
5. Admin khóa tài khoản: PUT `/api/v1/admin/users/{user_id}/status`
   - Body: { "is_active": false }
6. System cập nhật ApplicationUser.is_active = false
7. System invalidate tất cả JWT tokens của user (blacklist hoặc version increment)

**Alternative Flows**:
- 5a. Mở khóa: is_active = true
- 5b. Thay đổi role: PUT `/api/v1/admin/users/{user_id}/role` với body { "role": "Admin" }

**Postconditions**: User bị khóa hoặc role được thay đổi

#### UC-A03: Quản lý Thư viện Bài tập
**Actors**: Admin
**Preconditions**: Admin đã đăng nhập
**Main Flow**:
1. Admin GET `/api/v1/admin/exercises?muscle_group=Chest&difficulty=Intermediate`
2. System trả về filtered list of exercises
3. Admin tạo Exercise mới: POST `/api/v1/admin/exercises`
   - Body: {
       "name": "Barbell Bench Press",
       "muscle_group": "Chest",
       "difficulty_level": "Intermediate",
       "equipment": "Barbell",
       "description": "Classic compound chest exercise",
       "instructions": "Lie on bench, grip bar...",
       "calories_per_minute": 8.5
     }
4. System validate: name unique (hoặc cho phép duplicate với warning)
5. System tạo Exercise với created_by_admin_id = current admin
6. Admin upload ảnh minh họa: POST `/api/v1/admin/exercises/{id}/image` (multipart)
7. System upload to MinIO bucket "exercises", update image_url
8. System trả về Exercise với image_url

**Alternative Flows**:
- 3a. Update Exercise: PUT `/api/v1/admin/exercises/{id}`
- 3b. Delete Exercise: DELETE `/api/v1/admin/exercises/{id}`
  - System check: nếu Exercise đã được sử dụng trong ExerciseSession → soft delete hoặc return 400

**Postconditions**: Exercise library được cập nhật

#### UC-A04: Quản lý Thư viện Món ăn
**Actors**: Admin
**Preconditions**: Admin đã đăng nhập
**Main Flow**:
1. Admin POST `/api/v1/admin/foods` để tạo món ăn mới
   - Body: {
       "name": "Grilled Chicken Breast",
       "category": "Protein",
       "serving_size": 100,
       "serving_unit": "g",
       "calories_per_serving": 165,
       "protein_g": 31,
       "carbs_g": 0,
       "fat_g": 3.6,
       "fiber_g": 0,
       "description": "Lean protein source"
     }
2. System validate: serving_size > 0, calories >= 0, macros >= 0
3. System tạo FoodItem với created_by_admin_id
4. Admin upload ảnh: POST `/api/v1/admin/foods/{id}/image`
5. System upload to MinIO bucket "foods"

**Alternative Flows**:
- Tương tự UC-A03 (Update/Delete)

**Postconditions**: Food library được cập nhật

#### UC-A05: Quản lý Thử thách
**Actors**: Admin
**Preconditions**: Admin đã đăng nhập
**Main Flow**:
1. Admin POST `/api/v1/admin/challenges` để tạo thử thách mới
   - Body: {
       "title": "30-Day Running Challenge",
       "description": "Run 50km total in 30 days",
       "challenge_type": "Workout",
       "start_date": "2025-11-10",
       "end_date": "2025-12-10",
       "criteria": "Upload screenshot from running app showing total distance >= 50km",
       "max_participants": 100,
       "reward_description": "Certificate + 20% discount coupon"
     }
2. System tạo Challenge với status = Open, created_by_admin_id
3. Customers join và submit (theo UC-07)
4. Admin GET `/api/v1/admin/challenges/{id}/participants?status=PendingApproval`
5. System trả về list of ChallengeParticipation cần duyệt
6. Admin review từng participation: PUT `/api/v1/admin/challenges/participations/{id}/review`
   - Body: {
       "approved": true,
       "review_notes": "Great job! Distance verified."
     }
7. System validate: participation.status = PendingApproval
8. System cập nhật:
   - status = Completed (nếu approved) hoặc Failed
   - reviewed_by_admin_id = current admin
   - review_date = now
   - review_notes
   - completed_at = now (nếu approved)
9. System gửi email notification cho Customer
10. Admin đóng Challenge: PUT `/api/v1/admin/challenges/{id}` với status = Closed

**Alternative Flows**:
- 6a. Reject: approved = false → status = Failed

**Postconditions**: Challenge được quản lý, participations được duyệt

#### UC-A06: Xem Báo cáo Thử thách
**Actors**: Admin
**Preconditions**: Admin đã đăng nhập, có Challenge đã có participants
**Main Flow**:
1. Admin GET `/api/v1/admin/dashboard/challenge-stats`
2. System query:
   - Foreach Challenge: count Completed, Failed, PendingApproval, Joined
   - Calculate completion_rate = Completed / Total × 100
3. System trả về challenge stats với completion rates
4. Frontend render table hoặc chart

**Postconditions**: Admin thấy được hiệu quả của các thử thách



## Development Roadmap

### Phase 1: Foundation (Hiện tại)
- ✅ Solution structure với 4 projects (Domain, Application, Infrastructure, WebApi)
- ✅ JWT Authentication package installed
- ✅ EF Core với SQL Server configured
- ✅ Swagger/OpenAPI setup
- ⚠️ Remove placeholder `Class1.cs` files
- 🔄 Setup base entities và DbContext

### Phase 2: Authentication & User Management (UC-01, UC-02, UC-A02)
1. **Authentication Module**:
   - Implement `ApplicationUser` entity với password hashing (bcrypt)
   - JWT token generation: Access token (15 min) + Refresh token (7 days)
   - Endpoints: POST `/api/v1/auth/register`, `/api/v1/auth/login`, `/api/v1/auth/refresh`
   - OAuth2 integration: Google và Facebook login callback
   - Email confirmation workflow (tùy chọn)

2. **User Profile Management**:
   - Implement `UserProfile` entity (1-1 với ApplicationUser)
   - Endpoints: GET/PUT `/api/v1/users/profile`
   - Avatar upload: POST `/api/v1/users/avatar` (MinIO integration)
   - Validation: height > 0, weight > 0, date_of_birth < today

3. **Admin User Management** (UC-A02):
   - Endpoints: GET `/api/v1/admin/users` (pagination, search)
   - PUT `/api/v1/admin/users/{id}/status` (activate/deactivate)
   - PUT `/api/v1/admin/users/{id}/role` (change role)

### Phase 3: Workout Management Module (UC-03, UC-A05)
4. **Exercise Library** (Admin):
   - Implement `Exercise` entity
   - Endpoints: CRUD `/api/v1/admin/exercises`
   - Image upload cho bài tập (MinIO)
   - Filters: muscle_group, difficulty_level, equipment

5. **Workout Logging** (Customer):
   - Implement `WorkoutLog` và `ExerciseSession` entities
   - Endpoints:
     * POST `/api/v1/workouts` (create workout log)
     * POST `/api/v1/workouts/{id}/exercises` (add exercise session)
     * GET `/api/v1/workouts` (list user's workouts, filter by date range)
     * GET `/api/v1/workouts/{id}` (chi tiết buổi tập)
   - Auto-calculate total_duration_minutes và estimated_calories_burned

### Phase 4: Nutrition Management Module (UC-04, UC-A05)
6. **Food Library** (Admin):
   - Implement `FoodItem` entity
   - Endpoints: CRUD `/api/v1/admin/foods`
   - Image upload cho món ăn (MinIO)
   - Filters: category, search by name

7. **Nutrition Logging** (Customer):
   - Implement `NutritionLog` và `FoodEntry` entities
   - Endpoints:
     * GET `/api/v1/nutrition/daily/{date}` (lấy hoặc tạo NutritionLog cho ngày)
     * POST `/api/v1/nutrition/daily/{date}/entries` (thêm món ăn)
     * DELETE `/api/v1/nutrition/entries/{id}` (xóa món)
   - Auto-calculate totals (calories, protein, carbs, fat) khi thêm/xóa FoodEntry
   - Validate: quantity > 0

### Phase 5: Goal & Progress Tracking (UC-05, UC-06)
8. **Goal Management**:
   - Implement `Goal` và `ProgressRecord` entities
   - Endpoints:
     * POST `/api/v1/goals` (create goal)
     * GET `/api/v1/goals` (list user goals)
     * GET `/api/v1/goals/{id}` (chi tiết goal và progress records)
     * PUT `/api/v1/goals/{id}` (update goal)
     * DELETE `/api/v1/goals/{id}` (cancel goal)
   - Auto-create initial ProgressRecord khi tạo Goal mới

9. **Progress Tracking**:
   - Endpoints:
     * POST `/api/v1/goals/{id}/progress` (ghi tiến độ mới)
     * GET `/api/v1/goals/{id}/chart` (data cho biểu đồ line chart)
   - Calculate progress_percent: `(current - initial) / (target - initial) * 100`
   - Validate: record_date trong khoảng [start_date, end_date]

### Phase 6: Challenge Module (UC-A06, Customer Challenge Features)
10. **Challenge Management** (Admin):
    - Implement `Challenge` và `ChallengeParticipation` entities
    - Endpoints:
      * POST `/api/v1/admin/challenges` (create challenge)
      * PUT `/api/v1/admin/challenges/{id}` (update, open/close)
      * GET `/api/v1/admin/challenges/{id}/participants` (list participants)
      * PUT `/api/v1/admin/challenges/participations/{id}/review` (approve/reject submission)

11. **Challenge Participation** (Customer):
    - Endpoints:
      * GET `/api/v1/challenges` (list open challenges)
      * POST `/api/v1/challenges/{id}/join` (join challenge)
      * POST `/api/v1/challenges/{id}/submit` (nộp kết quả với submission_url)
      * GET `/api/v1/challenges/my-challenges` (challenges đã join)
    - State transitions: Joined → PendingApproval → Completed/Failed
    - Upload proof image/video (MinIO)

### Phase 7: Dashboard & Reporting (UC-A01)
12. **Admin Dashboard**:
    - Endpoints:
      * GET `/api/v1/admin/dashboard/stats` (tổng quan: total users, new users this month, workouts today)
      * GET `/api/v1/admin/dashboard/top-exercises` (top 5 exercises)
      * GET `/api/v1/admin/dashboard/top-foods` (top 5 foods)
      * GET `/api/v1/admin/dashboard/user-growth` (chart data: monthly new users)
      * GET `/api/v1/admin/dashboard/challenge-stats` (completion rates)
   - Implement caching (Redis/In-Memory) cho dashboard stats
   - Background job (Hangfire) để tổng hợp định kỳ

13. **Customer Reports** (tùy chọn):
    - Endpoints:
      * GET `/api/v1/reports/weekly-summary` (tổng kết tuần: workouts, nutrition, progress)
      * GET `/api/v1/reports/monthly-summary` (tổng kết tháng)

### Phase 8: Infrastructure & DevOps
14. **MinIO/Object Storage Integration**:
    - Service implementation: `IFileStorageService`
    - Upload/Download với URL signing (pre-signed URLs)
    - Image optimization và validation (MIME type, file size < 5MB)
    - Cleanup orphan files (background job)

15. **Email Notifications**:
    - Service implementation: `IEmailService`
    - Templates: Challenge approved, Challenge failed, Weekly summary
    - SMTP configuration hoặc SendGrid/MailGun integration

16. **Background Jobs** (Hangfire/Quartz.NET):
    - Daily: Close expired challenges, update goal statuses
    - Hourly: Refresh dashboard cache
    - Weekly: Cleanup orphan files, send weekly reports

17. **CI/CD Pipeline**:
    - GitHub Actions hoặc Jenkins
    - Build → Test → Dockerize
    - Deploy to staging/production
    - Database migrations automation

18. **Load Balancing & Scaling**:
    - NGINX reverse proxy
    - 2+ API instances (Docker Compose)
    - Ngrok cho public access (dev/testing)
    - Health check endpoints: GET `/health`, `/ready`

## Non-Functional Requirements (NFRs)

### 1. Hiệu năng (Performance)
- **Response Time**:
  - API endpoints: < 200ms cho queries đơn giản, < 1s cho aggregations
  - File upload: hỗ trợ upload concurrent, max 5MB/file
  - Dashboard stats: cache 1 giờ, refresh bằng background job
- **Database Optimization**:
  - Indexes trên: user_id, workout_date, log_date, email, exercise_id, food_item_id
  - Composite indexes cho queries phổ biến (user_id + workout_date)
  - Pagination bắt buộc cho list endpoints (default: page=1, size=20, max=100)
- **Rate Limiting**:
  - 100 requests/minute per IP cho anonymous endpoints
  - 1000 requests/minute per token cho authenticated users
  - 10 uploads/minute per user cho file endpoints

### 2. Bảo mật (Security)
- **Authentication & Authorization**:
  - JWT access token: 15 minutes TTL, HS256 algorithm
  - Refresh token: 7 days TTL, stored in database với token family (rotation)
  - Password hashing: bcrypt với cost factor = 12
  - OAuth2: Google/Facebook, validate state parameter để chống CSRF
- **Input Validation**:
  - FluentValidation hoặc Data Annotations cho tất cả DTOs
  - Sanitize user input (XSS prevention)
  - File upload: whitelist MIME types (image/jpeg, image/png), max 5MB
  - SQL Injection: sử dụng EF Core parameterized queries (KHÔNG dùng raw SQL)
- **Sensitive Data Protection**:
  - KHÔNG log passwords, tokens trong logs
  - HTTPS required cho production (TLS 1.2+)
  - Secrets management: User Secrets (dev), Azure Key Vault/AWS Secrets Manager (prod)
- **Audit Logging**:
  - Log tất cả admin actions (user deactivation, role change, challenge approval)
  - Format: JSON structured logs (timestamp, user_id, action, resource, ip_address)

### 3. Khả dụng & Mở rộng (Availability & Scalability)
- **Clean Architecture Benefits**:
  - Tách biệt concerns: Domain không phụ thuộc Infrastructure
  - Dễ test: mock repositories, services
  - Dễ thay thế: swap MinIO → AWS S3, SQL Server → PostgreSQL
- **Modular Design**:
  - Feature folders: mỗi module (Workout, Nutrition, Challenge) tự chứa đủ (Entities, DTOs, Repositories, Services)
  - Thêm module mới không ảnh hưởng modules cũ
- **Horizontal Scaling**:
  - Stateless API: không lưu session trong memory
  - Shared database connection pooling
  - Load balancer: NGINX/Azure App Gateway
- **High Availability**:
  - Database replication: master-slave hoặc read replicas
  - File storage: MinIO distributed mode hoặc cloud storage (S3, Azure Blob)
  - Health checks: `/health` (liveness), `/ready` (readiness)

### 4. Khả chuyển (Portability)
- **Database Agnostic**:
  - EF Core abstracts SQL Server specifics
  - Có thể migrate sang PostgreSQL, MySQL với ít thay đổi
  - Không dùng stored procedures (business logic ở Application layer)
- **File Storage Abstraction**:
  - Interface `IFileStorageService` với methods: UploadAsync, DownloadAsync, DeleteAsync
  - Implementations: MinIOStorageService, AzureBlobStorageService, S3StorageService
  - Configuration-driven: switch provider qua appsettings.json
- **Container-ready**:
  - Dockerfile cho API
  - Docker Compose cho local development (API + SQL Server + MinIO)
  - Kubernetes manifests (tùy chọn)

### 5. Quan sát (Observability)
- **Structured Logging**:
  - Serilog với sinks: Console (dev), File (prod), Seq/ELK (monitoring)
  - Log levels: Debug (dev only), Information, Warning, Error, Critical
  - Correlation IDs: track requests across services
- **Metrics**:
  - Application metrics: request count, response time, error rate
  - Business metrics: daily active users, workouts logged, challenges completed
  - Tools: Prometheus + Grafana, Application Insights
- **Tracing**:
  - OpenTelemetry hoặc Application Insights để trace requests
  - Identify slow queries, bottlenecks
- **Alerting**:
  - Cảnh báo khi: error rate > 5%, response time > 2s, database connection pool exhausted
  - Channels: Email, Slack, PagerDuty

### 6. Sao lưu & Khôi phục (Backup & Recovery)
- **Database Backup**:
  - Full backup: hàng tuần (Chủ nhật 2 AM)
  - Incremental backup: hàng ngày (2 AM)
  - Retention: 30 days
  - Test restore: quarterly
- **File Storage Backup**:
  - MinIO: replicate to secondary bucket
  - Cloud: versioning enabled (S3 versioning, Azure Blob soft delete)
- **Disaster Recovery**:
  - RTO (Recovery Time Objective): 4 hours
  - RPO (Recovery Point Objective): 24 hours
  - Runbook: documented restore procedures

### 7. Quốc tế hóa & Khả dụng (i18n & Accessibility)
- **Multi-language Support**:
  - API trả về error messages bằng tiếng Anh (hoặc theo Accept-Language header)
  - Frontend handle localization (vi-VN, en-US)
  - Database: lưu content bằng UTF-8
- **Time Zones**:
  - API nhận/trả dates ở UTC (ISO 8601 format: "2025-11-02T14:30:00Z")
  - Frontend convert sang timezone local của user
- **Accessibility** (cho Frontend):
  - WCAG 2.1 Level AA compliance
  - Keyboard navigation, screen reader support

## API Design Standards

### 1. Versioning
- **URL-based versioning**: `/api/v1/workouts`, `/api/v2/workouts`
- Breaking changes → new version (v2)
- Support 2 versions simultaneously, deprecate old version sau 6 tháng

### 2. Endpoints Naming Convention
- **RESTful principles**:
  - Resources: danh từ số nhiều (`/workouts`, `/exercises`, `/challenges`)
  - HTTP methods: GET (read), POST (create), PUT (update full), PATCH (partial update), DELETE
- **Examples**:
  ```
  GET    /api/v1/workouts              # List user's workouts
  POST   /api/v1/workouts              # Create workout log
  GET    /api/v1/workouts/{id}         # Get workout detail
  PUT    /api/v1/workouts/{id}         # Update workout
  DELETE /api/v1/workouts/{id}         # Delete workout
  
  POST   /api/v1/workouts/{id}/exercises     # Add exercise session
  DELETE /api/v1/workouts/exercises/{session_id}  # Remove exercise session
  
  GET    /api/v1/admin/users           # Admin: list all users
  PUT    /api/v1/admin/users/{id}/status     # Admin: activate/deactivate
  ```

### 3. Request & Response Format
- **Content-Type**: `application/json` (UTF-8)
- **Date/Time Format**: ISO 8601 UTC (`"2025-11-02T14:30:00Z"`)
- **Request Body** (POST/PUT):
  ```json
  {
    "workout_date": "2025-11-02",
    "notes": "Chest and triceps day"
  }
  ```
- **Success Response** (200 OK, 201 Created):
  ```json
  {
    "success": true,
    "data": {
      "workout_log_id": 123,
      "workout_date": "2025-11-02",
      "total_duration_minutes": 60,
      "exercise_sessions": [...]
    },
    "message": "Workout created successfully"
  }
  ```
- **Error Response** (4xx, 5xx):
  ```json
  {
    "success": false,
    "error": {
      "code": "VALIDATION_ERROR",
      "message": "Invalid input data",
      "details": [
        {
          "field": "workout_date",
          "message": "Date cannot be in the future"
        }
      ]
    },
    "timestamp": "2025-11-02T14:30:00Z",
    "path": "/api/v1/workouts"
  }
  ```

### 4. Pagination, Filtering, Sorting
- **Pagination** (query params):
  ```
  GET /api/v1/workouts?page=1&size=20
  
  Response:
  {
    "success": true,
    "data": [...],
    "pagination": {
      "page": 1,
      "size": 20,
      "total_items": 150,
      "total_pages": 8
    }
  }
  ```
- **Filtering**:
  ```
  GET /api/v1/workouts?start_date=2025-11-01&end_date=2025-11-30
  GET /api/v1/exercises?muscle_group=Chest&difficulty=Intermediate
  ```
- **Sorting**:
  ```
  GET /api/v1/workouts?sort_by=workout_date&order=desc
  ```

### 5. HTTP Status Codes
- **Success**:
  - 200 OK: GET, PUT, PATCH successful
  - 201 Created: POST successful (return Location header với URL của resource mới)
  - 204 No Content: DELETE successful
- **Client Errors**:
  - 400 Bad Request: Validation error, malformed request
  - 401 Unauthorized: Missing hoặc invalid token
  - 403 Forbidden: Không đủ quyền (ví dụ: Customer cố gọi admin endpoint)
  - 404 Not Found: Resource không tồn tại
  - 409 Conflict: Business rule violation (ví dụ: email đã tồn tại)
  - 422 Unprocessable Entity: Semantic errors
- **Server Errors**:
  - 500 Internal Server Error: Unhandled exception
  - 503 Service Unavailable: Database down, MinIO unreachable

### 6. Error Codes (Internal)
- **Authentication**: `AUTH_INVALID_CREDENTIALS`, `AUTH_TOKEN_EXPIRED`, `AUTH_EMAIL_NOT_CONFIRMED`
- **Authorization**: `FORBIDDEN_INSUFFICIENT_PERMISSIONS`, `FORBIDDEN_RESOURCE_ACCESS`
- **Validation**: `VALIDATION_ERROR`, `VALIDATION_REQUIRED_FIELD`, `VALIDATION_INVALID_FORMAT`
- **Business Logic**: `BUSINESS_GOAL_DATE_INVALID`, `BUSINESS_CHALLENGE_CLOSED`, `BUSINESS_ALREADY_JOINED`
- **Resource**: `RESOURCE_NOT_FOUND`, `RESOURCE_ALREADY_EXISTS`, `RESOURCE_CONFLICT`

### 7. API Documentation (Swagger/OpenAPI)
- **Swagger UI**: Enabled ở Development mode only (`app.UseSwagger()` if `env.IsDevelopment()`)
- **Annotations**:
  ```csharp
  /// <summary>
  /// Create a new workout log
  /// </summary>
  /// <param name="request">Workout log creation request</param>
  /// <returns>Created workout log</returns>
  /// <response code="201">Workout created successfully</response>
  /// <response code="400">Invalid input data</response>
  /// <response code="401">Unauthorized - invalid or missing token</response>
  [HttpPost]
  [Authorize(Roles = "Customer")]
  [ProducesResponseType(typeof(WorkoutLogDto), StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> CreateWorkout([FromBody] CreateWorkoutRequest request)
  ```
- **Security Schemes**: Document JWT Bearer trong Swagger
- **Examples**: Provide request/response examples cho mỗi endpoint

### 8. File Upload Endpoints
- **Content-Type**: `multipart/form-data`
- **Example**:
  ```
  POST /api/v1/users/avatar
  Content-Type: multipart/form-data
  
  FormData:
  - file: [binary image data]
  
  Response (201):
  {
    "success": true,
    "data": {
      "avatar_url": "https://minio.example.com/avatars/user123.jpg"
    }
  }
  ```
- **Validation**: MIME type whitelist, file size < 5MB, image dimensions (optional)



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
