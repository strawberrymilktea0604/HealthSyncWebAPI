# TÀI LIỆU TEST CASE - DỰ ÁN HEALTHSYNC

> **Dự án:** HealthSync - Nền tảng quản lý sức khỏe cá nhân  
> **Phiên bản:** 1.0  
> **Ngày tạo:** 06/01/2026  
> **Người tạo:** HealthSync Team

---

## TỔNG QUAN

Tài liệu này mô tả các kịch bản kiểm thử chức năng (Functional Test Cases) cho hệ thống HealthSync API. Các test case được tổ chức theo Module và Chức năng chính.

### Quy ước đánh số Test Case ID:
- `TC-AUTH-XXX`: Module Xác thực (Authentication)
- `TC-USER-XXX`: Module Quản lý người dùng (User Management)  
- `TC-GOAL-XXX`: Module Quản lý mục tiêu (Goal Management)
- `TC-WORK-XXX`: Module Nhật ký luyện tập (Workout Logs)
- `TC-NUTR-XXX`: Module Nhật ký dinh dưỡng (Nutrition Logs)
- `TC-CHAL-XXX`: Module Thử thách cộng đồng (Challenges)
- `TC-FORUM-XXX`: Module Diễn đàn (Forum)
- `TC-LEAD-XXX`: Module Bảng xếp hạng (Leaderboard)
- `TC-ADMIN-XXX`: Module Quản trị (Admin)

### Trạng thái Test Case:
- **Pass**: Test case đã vượt qua
- **Fail**: Test case thất bại
- **Pending**: Chưa thực hiện
- **Blocked**: Bị chặn bởi lỗi khác

---

## MODULE 1: XÁC THỰC (AUTHENTICATION)

### Chức năng: Đăng ký tài khoản (Register)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-AUTH-001 | Đăng ký với thông tin hợp lệ | 1. Gửi POST `/api/v1/auth/register` với email, password, fullName hợp lệ | `{"email": "test@example.com", "password": "Password123!", "fullName": "Test User"}` | HTTP 200, trả về AccessToken, RefreshToken, Email, Role="Customer", FullName | | Pending | Happy path |
| TC-AUTH-002 | Đăng ký với email đã tồn tại | 1. Đăng ký user mới 2. Đăng ký lại với cùng email | `{"email": "existing@example.com", "password": "Password123!", "fullName": "Test User"}` | HTTP 400, message: "Email already registered" | | Pending | Validation trùng email |
| TC-AUTH-003 | Đăng ký với email không hợp lệ | 1. Gửi POST với email sai format | `{"email": "invalid-email", "password": "Password123!", "fullName": "Test"}` | HTTP 400, validation error về email format | | Pending | Email format validation |
| TC-AUTH-004 | Đăng ký với password yếu | 1. Gửi POST với password không đủ mạnh | `{"email": "test@example.com", "password": "123", "fullName": "Test"}` | HTTP 400, message yêu cầu password mạnh hơn | | Pending | Password strength |
| TC-AUTH-005 | Đăng ký thiếu trường bắt buộc | 1. Gửi POST thiếu fullName | `{"email": "test@example.com", "password": "Password123!"}` | HTTP 400, validation error về fullName required | | Pending | Required fields |

### Chức năng: Đăng nhập (Login)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-AUTH-006 | Đăng nhập với thông tin hợp lệ | 1. Gửi POST `/api/v1/auth/login` với credentials đúng | `{"email": "test@example.com", "password": "Password123!"}` | HTTP 200, trả về AccessToken, RefreshToken, thông tin user | | Pending | Happy path |
| TC-AUTH-007 | Đăng nhập với sai mật khẩu | 1. Gửi POST với password sai | `{"email": "test@example.com", "password": "WrongPassword"}` | HTTP 401, message: "Invalid credentials" | | Pending | Wrong password |
| TC-AUTH-008 | Đăng nhập với email không tồn tại | 1. Gửi POST với email chưa đăng ký | `{"email": "notexist@example.com", "password": "Password123!"}` | HTTP 401, message: "Invalid credentials" | | Pending | Email not found |
| TC-AUTH-009 | Đăng nhập với tài khoản bị khóa | 1. Admin khóa tài khoản 2. User đăng nhập | Email của user bị khóa | HTTP 401/403, message về tài khoản bị khóa | | Pending | Deactivated account |

### Chức năng: Làm mới Token (Refresh Token)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-AUTH-010 | Làm mới token với refresh token hợp lệ | 1. Đăng nhập lấy refresh token 2. Gửi POST `/api/v1/auth/refresh` | `{"refreshToken": "valid_refresh_token"}` | HTTP 200, trả về AccessToken mới, RefreshToken mới | | Pending | Happy path |
| TC-AUTH-011 | Làm mới token với refresh token không hợp lệ | 1. Gửi POST với token sai | `{"refreshToken": "invalid_refresh_token"}` | HTTP 401, message: "Invalid refresh token" | | Pending | Invalid token |
| TC-AUTH-012 | Làm mới token với refresh token hết hạn | 1. Gửi POST với token đã hết hạn | Refresh token đã hết hạn (> 7 ngày) | HTTP 401, message: "Invalid refresh token" | | Pending | Expired token |

### Chức năng: Đăng nhập OAuth2 (Google Login)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-AUTH-013 | Đăng nhập Google với token hợp lệ | 1. Gửi POST `/api/v1/auth/google` với Google ID token | `{"token": "valid_google_id_token"}` | HTTP 200, trả về AccessToken, RefreshToken | | Pending | OAuth2 happy path |
| TC-AUTH-014 | Đăng nhập Google với token không hợp lệ | 1. Gửi POST với Google token sai | `{"token": "invalid_google_token"}` | HTTP 401, message về lỗi xác thực Google | | Pending | Invalid OAuth token |

---

## MODULE 2: QUẢN LÝ NGƯỜI DÙNG (USER MANAGEMENT)

### Chức năng: Xem hồ sơ cá nhân (View Profile)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-USER-001 | Xem hồ sơ khi đã đăng nhập và có profile | 1. Đăng nhập 2. GET `/api/v1/users/profile` | Authorization: Bearer token | HTTP 200, trả về thông tin profile (fullName, dob, gender, height, weight, activityLevel, avatarUrl, contributionPoints) | | Pending | Happy path |
| TC-USER-002 | Xem hồ sơ khi profile chưa được tạo | 1. Đăng nhập user mới chưa có profile 2. GET `/api/v1/users/profile` | Authorization: Bearer token | HTTP 404, message: "Profile not found" | | Pending | Profile not exists |
| TC-USER-003 | Xem hồ sơ khi chưa đăng nhập | 1. GET `/api/v1/users/profile` không có token | Không có Authorization header | HTTP 401, Unauthorized | | Pending | No auth |

### Chức năng: Cập nhật hồ sơ (Update Profile)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-USER-004 | Cập nhật profile với dữ liệu hợp lệ | 1. Đăng nhập 2. PUT `/api/v1/users/profile` | `{"fullName": "Updated Name", "dateOfBirth": "1990-01-01", "gender": "Male", "heightCm": 180, "currentWeightKg": 75, "activityLevel": "VeryActive"}` | HTTP 200, profile được cập nhật thành công | | Pending | Happy path |
| TC-USER-005 | Cập nhật profile với fullName rỗng | 1. Đăng nhập 2. PUT với fullName = "" | `{"fullName": "", ...}` | HTTP 400, validation error về FullName | | Pending | Empty name validation |
| TC-USER-006 | Cập nhật profile với heightCm <= 0 | 1. PUT với heightCm = -10 | `{"heightCm": -10, ...}` | HTTP 400, validation error về height | | Pending | Negative height |
| TC-USER-007 | Cập nhật profile với weightKg <= 0 | 1. PUT với currentWeightKg = 0 | `{"currentWeightKg": 0, ...}` | HTTP 400, validation error về weight | | Pending | Zero weight |

### Chức năng: Tải lên ảnh đại diện (Upload Avatar)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-USER-008 | Upload avatar với file hợp lệ | 1. Đăng nhập 2. POST `/api/v1/users/avatar` với file ảnh | File ảnh JPEG/PNG < 5MB | HTTP 200, trả về avatarUrl mới | | Pending | Happy path |
| TC-USER-009 | Upload avatar không có file | 1. POST không gửi file | FormData rỗng | HTTP 400, message: "No file uploaded" | | Pending | No file |
| TC-USER-010 | Upload avatar với file quá lớn | 1. POST với file > 5MB | File ảnh 10MB | HTTP 400, message về giới hạn file size | | Pending | File size limit |
| TC-USER-011 | Upload avatar với định dạng không hợp lệ | 1. POST với file .exe hoặc .pdf | File không phải ảnh | HTTP 400, message về MIME type không hợp lệ | | Pending | Invalid MIME type |
| TC-USER-012 | Upload avatar cập nhật profile hiện có | 1. User đã có profile 2. Upload avatar mới | File ảnh hợp lệ | HTTP 200, avatarUrl được cập nhật, ảnh cũ bị xóa | | Pending | Update existing |
| TC-USER-013 | Upload avatar tạo profile mới nếu chưa có | 1. User chưa có profile 2. Upload avatar | File ảnh hợp lệ | HTTP 200, tự động tạo profile với avatarUrl | | Pending | Auto create profile |

---

## MODULE 3: QUẢN LÝ MỤC TIÊU (GOAL MANAGEMENT)

### Chức năng: Tạo mục tiêu mới (Create Goal)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-GOAL-001 | Tạo mục tiêu giảm cân hợp lệ | 1. Đăng nhập 2. POST `/api/v1/goals` | `{"goalType": "WeightLoss", "targetValue": 70.0, "unit": "kg", "startDate": "2026-01-10", "endDate": "2026-04-10"}` | HTTP 201, trả về goal với status=InProgress, tự động tạo ProgressRecord đầu tiên | | Pending | Happy path - WeightLoss |
| TC-GOAL-002 | Tạo mục tiêu tăng cân hợp lệ | 1. POST với goalType = WeightGain | `{"goalType": "WeightGain", "targetValue": 75.0, ...}` | HTTP 201, goal được tạo thành công | | Pending | Happy path - WeightGain |
| TC-GOAL-003 | Tạo mục tiêu với endDate trước startDate | 1. POST với endDate < startDate | `{"startDate": "2026-03-01", "endDate": "2026-01-01", ...}` | HTTP 400, validation error về ngày | | Pending | Invalid date range |
| TC-GOAL-004 | Tạo mục tiêu với targetValue âm | 1. POST với targetValue = -10 | `{"targetValue": -10, ...}` | HTTP 400, validation error về giá trị mục tiêu | | Pending | Negative target |

### Chức năng: Xem danh sách mục tiêu (Get My Goals)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-GOAL-005 | Xem danh sách mục tiêu của user | 1. Đăng nhập 2. GET `/api/v1/goals` | Authorization header | HTTP 200, trả về danh sách goals của user với pagination | | Pending | Happy path |
| TC-GOAL-006 | Xem danh sách khi chưa có mục tiêu | 1. User mới, chưa tạo goal 2. GET `/api/v1/goals` | Authorization header | HTTP 200, trả về danh sách rỗng | | Pending | Empty list |

### Chức năng: Xem chi tiết mục tiêu (Get Goal Details)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-GOAL-007 | Xem chi tiết goal khi goal tồn tại | 1. GET `/api/v1/goals/{goalId}` | goalId = 1 (goal của user) | HTTP 200, trả về goal với danh sách ProgressRecords | | Pending | Happy path |
| TC-GOAL-008 | Xem chi tiết goal không tồn tại | 1. GET với goalId không có | goalId = 999 | HTTP 404, message: "Goal not found" | | Pending | Not found |
| TC-GOAL-009 | Xem chi tiết goal của user khác | 1. GET goal thuộc user khác | goalId thuộc sở hữu user khác | HTTP 404 hoặc 403 | | Pending | Access denied |

### Chức năng: Ghi tiến độ (Record Progress)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-GOAL-010 | Ghi tiến độ mới hợp lệ | 1. POST `/api/v1/goals/{goalId}/progress` | `{"recordDate": "2026-01-15", "recordedValue": 71.5, "weightKg": 71.5, "notes": "Lost 1kg"}` | HTTP 201, trả về ProgressRecord mới | | Pending | Happy path |
| TC-GOAL-011 | Ghi tiến độ ngoài khoảng thời gian goal | 1. POST với recordDate ngoài [startDate, endDate] | recordDate vượt quá endDate | HTTP 400, validation error | | Pending | Date out of range |
| TC-GOAL-012 | Ghi tiến độ trùng ngày | 1. Đã có progress ngày 15/01 2. POST thêm ngày 15/01 | Duplicate recordDate | HTTP 400, message về duplicate | | Pending | Duplicate date |

### Chức năng: Xem biểu đồ tiến độ (Progress Chart)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-GOAL-013 | Lấy dữ liệu biểu đồ tiến độ | 1. GET `/api/v1/goals/{goalId}/chart` | goalId có nhiều ProgressRecords | HTTP 200, trả về ChartData với GoalId, ProgressPercent, danh sách ProgressRecords | | Pending | Happy path |
| TC-GOAL-014 | Tính đúng % tiến độ | 1. Goal: 70kg target, initial 75kg 2. Current: 72.5kg | Dữ liệu như mô tả | progressPercent = 50% (giảm 2.5kg/5kg) | | Pending | Progress calculation |

---

## MODULE 4: NHẬT KÝ LUYỆN TẬP (WORKOUT LOGS)

### Chức năng: Tạo nhật ký luyện tập (Create Workout Log)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-WORK-001 | Tạo workout log với exercise sessions | 1. POST `/api/v1/workouts` | `{"workoutDate": "2026-01-06", "notes": "Chest day", "exerciseSessions": [{"exerciseId": 1, "sets": 4, "reps": 10, "weightKg": 80, "restSeconds": 90, "rpe": 8, "orderIndex": 1}]}` | HTTP 201, trả về WorkoutLog với tổng duration, calories, danh sách sessions | | Pending | Happy path |
| TC-WORK-002 | Tạo workout log không có sessions | 1. POST với exerciseSessions rỗng | `{"workoutDate": "2026-01-06", "exerciseSessions": []}` | HTTP 400, validation error về ít nhất 1 session | | Pending | Empty sessions |
| TC-WORK-003 | Tạo workout log khi chưa đăng nhập | 1. POST không có Authorization | Không có token | HTTP 401, message: "User ID not found in token" | | Pending | No auth |
| TC-WORK-004 | Tạo workout log với exerciseId không tồn tại | 1. POST với exerciseId = 999 | exerciseId không có trong database | HTTP 400, message về exercise not found | | Pending | Invalid exerciseId |
| TC-WORK-005 | Tạo workout log với sets <= 0 | 1. POST với sets = 0 | `{"sets": 0, ...}` | HTTP 400, validation error | | Pending | Invalid sets |
| TC-WORK-006 | Tạo workout log với reps <= 0 | 1. POST với reps = -5 | `{"reps": -5, ...}` | HTTP 400, validation error | | Pending | Invalid reps |

### Chức năng: Xem danh sách nhật ký luyện tập (Get Workout Logs)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-WORK-007 | Lấy danh sách workout logs với pagination | 1. GET `/api/v1/workouts?page=1&size=20` | Query params hợp lệ | HTTP 200, trả về PaginatedResult với Items, TotalItems, TotalPages | | Pending | Happy path |
| TC-WORK-008 | Lấy workout logs với filter theo ngày | 1. GET với startDate và endDate | `?startDate=2026-01-01&endDate=2026-01-31` | HTTP 200, chỉ trả về logs trong khoảng thời gian | | Pending | Date filter |
| TC-WORK-009 | Lấy workout logs với pageNumber <= 0 | 1. GET với page = 0 | `?page=0` | HTTP 400, message: "Page number must be >= 1" | | Pending | Invalid page |
| TC-WORK-010 | Lấy workout logs với pageSize > 100 | 1. GET với size = 150 | `?size=150` | HTTP 400, message: "Page size must be between 1 and 100" | | Pending | Exceeds max page size |
| TC-WORK-011 | Lấy workout logs với startDate > endDate | 1. GET với startDate sau endDate | `?startDate=2026-12-31&endDate=2026-01-01` | HTTP 400, message: "Start date must be before or equal to end date" | | Pending | Invalid date range |

---

## MODULE 5: NHẬT KÝ DINH DƯỠNG (NUTRITION LOGS)

### Chức năng: Lấy nhật ký dinh dưỡng theo ngày (Get Daily Log)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-NUTR-001 | Lấy nutrition log ngày hợp lệ | 1. GET `/api/v1/nutrition/daily/2026-01-06` | Date format YYYY-MM-DD | HTTP 200, trả về NutritionLog với TotalCalories, TotalProtein, Macros, EntriesByMeal | | Pending | Happy path |
| TC-NUTR-002 | Lấy nutrition log với date format sai | 1. GET `/api/v1/nutrition/daily/invalid-date` | "invalid-date" | HTTP 400, message: "Invalid date format. Use YYYY-MM-DD." | | Pending | Invalid date format |
| TC-NUTR-003 | Tự động tạo log mới nếu chưa có | 1. GET với ngày chưa có log | Ngày mới chưa có dữ liệu | HTTP 200, trả về NutritionLog mới với totals = 0 | | Pending | Auto create |

### Chức năng: Thêm món ăn (Add Food Entry)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-NUTR-004 | Thêm food entry hợp lệ | 1. POST `/api/v1/nutrition/daily/2026-01-06/entries` | `{"foodItemId": 1, "mealType": "Breakfast", "quantity": 2.0, "notes": "Delicious"}` | HTTP 200, trả về FoodEntry với calories tự động tính (quantity × caloriesPerServing) | | Pending | Happy path |
| TC-NUTR-005 | Thêm food entry với quantity <= 0 | 1. POST với quantity = 0 | `{"quantity": 0, ...}` | HTTP 400, validation error | | Pending | Invalid quantity |
| TC-NUTR-006 | Thêm food entry với foodItemId không tồn tại | 1. POST với foodItemId = 999 | foodItemId không có | HTTP 400/404, message về food item not found | | Pending | Invalid foodItemId |
| TC-NUTR-007 | Tự động tính macros khi thêm entry | 1. FoodItem có protein=31g/serving 2. Thêm quantity=1.5 | quantity = 1.5 | proteinG = 46.5g (31 × 1.5) | | Pending | Auto calculate macros |

### Chức năng: Xóa món ăn (Delete Food Entry)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-NUTR-008 | Xóa food entry tồn tại | 1. DELETE `/api/v1/nutrition/entries/{entryId}` | entryId hợp lệ | HTTP 204 NoContent | | Pending | Happy path |
| TC-NUTR-009 | Xóa food entry không tồn tại | 1. DELETE với entryId = 999 | entryId không có | HTTP 404, message: "Food entry with ID 999 not found" | | Pending | Not found |
| TC-NUTR-010 | Xóa entry của user khác | 1. DELETE entry không thuộc user hiện tại | entryId thuộc user khác | HTTP 404 (không tìm thấy trong scope của user) | | Pending | Access control |

### Chức năng: Xem danh sách nutrition logs (Get Nutrition Logs)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-NUTR-011 | Lấy danh sách nutrition logs với pagination | 1. GET `/api/v1/nutrition?page=1&size=10` | Query params hợp lệ | HTTP 200, trả về PaginatedResult với Items, TotalItems | | Pending | Happy path |

---

## MODULE 6: THỬ THÁCH CỘNG ĐỒNG (CHALLENGES)

### Chức năng: Xem thử thách đang mở (Get Open Challenges)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-CHAL-001 | Lấy danh sách challenges đang mở | 1. GET `/api/v1/challenges?page=1&size=20` | Query params hợp lệ | HTTP 200, trả về danh sách challenges có status=Open, chưa hết hạn | | Pending | Happy path |
| TC-CHAL-002 | Lấy challenges với page <= 0 | 1. GET với page = 0 | `?page=0` | HTTP 400, message: "Page number must be >= 1" | | Pending | Invalid page |

### Chức năng: Xem chi tiết thử thách (Get Challenge Details)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-CHAL-003 | Xem chi tiết challenge tồn tại | 1. GET `/api/v1/challenges/{challengeId}` | challengeId = 1 | HTTP 200, trả về Challenge với Title, Description, ChallengeType, StartDate, EndDate, Criteria, Status | | Pending | Happy path |
| TC-CHAL-004 | Xem chi tiết challenge không tồn tại | 1. GET với challengeId = 999 | challengeId không có | HTTP 404, message: "Challenge not found" | | Pending | Not found |

### Chức năng: Tham gia thử thách (Join Challenge)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-CHAL-005 | Tham gia challenge đang mở | 1. POST `/api/v1/challenges/{id}/join` | challengeId có status=Open | HTTP 200, tạo ChallengeParticipation với status=Joined | | Pending | Happy path |
| TC-CHAL-006 | Tham gia challenge đã đóng | 1. POST vào challenge có status=Closed | challengeId đã Closed | HTTP 400, message: "Challenge is closed" | | Pending | Closed challenge |
| TC-CHAL-007 | Tham gia challenge đã join rồi | 1. User đã join 2. POST join lần nữa | User đã có participation | HTTP 400, message: "Already joined" | | Pending | Already joined |
| TC-CHAL-008 | Tham gia khi challenge đã đủ người | 1. Challenge có maxParticipants đã đạt | Challenge đầy slots | HTTP 400, message về đã đủ người tham gia | | Pending | Max participants |

### Chức năng: Nộp kết quả thử thách (Submit Challenge)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-CHAL-009 | Nộp kết quả thành công | 1. POST `/api/v1/challenges/{id}/submit` | `{"submissionText": "Completed", "submissionUrl": "https://...proof.jpg"}` | HTTP 200, status chuyển sang PendingApproval, message về chờ admin duyệt | | Pending | Happy path |
| TC-CHAL-010 | Nộp kết quả khi chưa join | 1. POST submit mà chưa join challenge | User chưa join | HTTP 400, message: "Challenge not found" hoặc "You haven't joined" | | Pending | Not joined |
| TC-CHAL-011 | Nộp kết quả khi đã submit rồi | 1. User đã submit 2. Submit lại | Status đã là PendingApproval | HTTP 400, message: "Already submitted" | | Pending | Already submitted |

### Chức năng: Xem thử thách đã tham gia (My Participations)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-CHAL-012 | Xem danh sách participations của user | 1. GET `/api/v1/challenges/my-challenges` | Authorization header | HTTP 200, trả về danh sách participations với status (Joined, PendingApproval, Completed, Failed) | | Pending | Happy path |
| TC-CHAL-013 | Xem khi chưa đăng nhập | 1. GET không có token | Không Authorization | HTTP 500 hoặc 401 | | Pending | No auth |

---

## MODULE 7: DIỄN ĐÀN (FORUM)

### Chức năng: Xem danh sách chuyên mục (Get Categories)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-FORUM-001 | Lấy danh sách forum categories | 1. GET `/api/v1/forum/categories` | Không cần auth | HTTP 200, trả về danh sách categories với Name, Description, DisplayOrder | | Pending | Happy path |
| TC-FORUM-002 | Lấy categories khi database rỗng | 1. Không có category nào 2. GET | Database rỗng | HTTP 200, trả về danh sách rỗng | | Pending | Empty list |

### Chức năng: Xem chi tiết bài đăng (Get Post Details)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-FORUM-003 | Xem chi tiết post tồn tại | 1. GET `/api/v1/forum/posts/{postId}` | postId = 1 | HTTP 200, trả về Post với Title, Content, CategoryName, UserName, IsPinned, IsLocked, ReplyCount, Replies (không bao gồm hidden) | | Pending | Happy path |
| TC-FORUM-004 | Xem post không tồn tại | 1. GET với postId = 999 | postId không có | HTTP 404, message: "Post not found" | | Pending | Not found |
| TC-FORUM-005 | Chỉ hiển thị replies không bị hidden | 1. Post có 2 replies (1 hidden, 1 visible) | postId có hidden replies | HTTP 200, ReplyCount = 1, Replies chỉ có 1 item | | Pending | Filter hidden replies |

### Chức năng: Tạo bài đăng (Create Post)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-FORUM-006 | Tạo post mới hợp lệ | 1. POST `/api/v1/forum/posts` | `{"categoryId": 1, "title": "New Post", "content": "Post content"}` | HTTP 201, Post được tạo với isPinned=false, isLocked=false | | Pending | Happy path |
| TC-FORUM-007 | Tạo post với title rỗng | 1. POST với title = "" | `{"title": "", "content": "..."}` | HTTP 400, validation error về title | | Pending | Empty title |
| TC-FORUM-008 | Tạo post với categoryId không tồn tại | 1. POST với categoryId = 999 | categoryId không có | HTTP 400/404, message về category not found | | Pending | Invalid category |

### Chức năng: Trả lời bài đăng (Create Reply)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-FORUM-009 | Tạo reply cho post không bị khóa | 1. POST `/api/v1/forum/posts/{postId}/replies` | `{"content": "Reply content"}` | HTTP 201, Reply được tạo với isHidden=false | | Pending | Happy path |
| TC-FORUM-010 | Tạo reply cho post bị khóa | 1. POST reply vào post có isLocked=true | Post.isLocked = true | HTTP 400, message: "Post is locked" hoặc "Cannot reply to locked post" | | Pending | Locked post |
| TC-FORUM-011 | Tạo reply với content rỗng | 1. POST với content = "" | `{"content": ""}` | HTTP 400, validation error về content | | Pending | Empty content |

---

## MODULE 8: BẢNG XẾP HẠNG (LEADERBOARD)

### Chức năng: Xem top leaderboard (Get Top Users)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-LEAD-001 | Lấy top 10 users theo điểm đóng góp | 1. GET `/api/v1/leaderboard/top?limit=10` | limit = 10 | HTTP 200, trả về danh sách users sắp xếp theo ContributionPoints giảm dần | | Pending | Happy path |
| TC-LEAD-002 | Lấy top với limit <= 0 | 1. GET với limit = 0 | limit = 0 | HTTP 400, message: "Limit must be between 1 and 100" | | Pending | Invalid limit |
| TC-LEAD-003 | Lấy top với limit > 100 | 1. GET với limit = 101 | limit = 101 | HTTP 400, message: "Limit must be between 1 and 100" | | Pending | Exceeds max limit |

### Chức năng: Xem xếp hạng của mình (Get My Ranking)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-LEAD-004 | Xem xếp hạng của user đang đăng nhập | 1. GET `/api/v1/leaderboard/my-ranking` | Authorization header | HTTP 200, trả về UserRankDto với UserId, UserName, TotalPoints, RankPosition | | Pending | Happy path |
| TC-LEAD-005 | Xem xếp hạng khi chưa có trong leaderboard | 1. User mới chưa có entry 2. GET | User chưa có leaderboard entry | HTTP 404, message: "Leaderboard entry not found" | | Pending | Not found |
| TC-LEAD-006 | Xem xếp hạng khi chưa đăng nhập | 1. GET không có token | Không Authorization | HTTP 500 (missing user context) | | Pending | No auth |

### Chức năng: Xem bảng xếp hạng đầy đủ (Get Full Leaderboard)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-LEAD-007 | Lấy leaderboard với pagination | 1. GET `/api/v1/leaderboard?page=1&size=20` | Query params hợp lệ | HTTP 200, trả về PaginatedResult với LeaderboardEntryDto (UserId, UserName, TotalPoints, RankPosition) | | Pending | Happy path |
| TC-LEAD-008 | Lấy leaderboard với page <= 0 | 1. GET với page = 0 | page = 0 | HTTP 400, message: "Page number must be >= 1" | | Pending | Invalid page |
| TC-LEAD-009 | Lấy leaderboard với size <= 0 | 1. GET với size = 0 | size = 0 | HTTP 400, message: "Page size must be between 1 and 100" | | Pending | Invalid size |
| TC-LEAD-010 | Lấy leaderboard với size > 100 | 1. GET với size = 101 | size = 101 | HTTP 400, message: "Page size must be between 1 and 100" | | Pending | Exceeds max size |

---

## MODULE 9: QUẢN TRỊ - ADMIN

### 9.1 Dashboard (UC-A01)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-ADMIN-001 | Lấy thống kê tổng quan (Admin) | 1. Đăng nhập Admin 2. GET `/api/v1/admin/dashboard/stats` | Admin Authorization | HTTP 200, trả về TotalActiveUsers, NewUsersThisMonth, WorkoutsLoggedToday | | Pending | Happy path |
| TC-ADMIN-002 | Lấy stats khi service lỗi | 1. Database lỗi 2. GET stats | Service trả về failure | HTTP 500, message về lỗi database | | Pending | Service failure |
| TC-ADMIN-003 | Lấy detailed stats | 1. GET `/api/v1/admin/dashboard/detailed` | Admin Authorization | HTTP 200, trả về thêm NutritionLogsToday, ForumPosts/Replies, OpenChallenges, PendingSubmissions | | Pending | Detailed stats |
| TC-ADMIN-004 | Lấy top content (exercises, categories) | 1. GET `/api/v1/admin/dashboard/top-content` | Admin Authorization | HTTP 200, trả về TopExercises, TopForumCategories | | Pending | Top content |
| TC-ADMIN-005 | Lấy top users theo contribution points | 1. GET `/api/v1/admin/dashboard/top-users` | Admin Authorization | HTTP 200, trả về danh sách users sắp xếp theo ContributionPoints | | Pending | Top contributors |

### 9.2 Quản lý người dùng (UC-A02)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-ADMIN-006 | Lấy danh sách users với pagination | 1. GET `/api/v1/admin/users?page=1&size=20` | Admin auth, query params | HTTP 200, trả về PaginatedResult với AdminUserDto (Id, Email, Role, IsActive, FullName, CreatedAt) | | Pending | Happy path |
| TC-ADMIN-007 | Lấy users với filter search | 1. GET với search param | `?search=john` | HTTP 200, chỉ trả về users có email/name chứa "john" | | Pending | Search filter |
| TC-ADMIN-008 | Lấy users với filter role | 1. GET với role param | `?role=Customer` | HTTP 200, chỉ trả về users có role Customer | | Pending | Role filter |
| TC-ADMIN-009 | Xem chi tiết user | 1. GET `/api/v1/admin/users/{userId}` | userId hợp lệ | HTTP 200, trả về User + Profile + Stats (totalWorkouts, totalNutritionLogs, totalGoals, totalChallenges) | | Pending | User details |
| TC-ADMIN-010 | Xem chi tiết user không tồn tại | 1. GET với userId = 999 | userId không có | HTTP 404 | | Pending | User not found |

### 9.3 Quản lý thư viện bài tập (UC-A03)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-ADMIN-011 | Lấy exercise theo ID | 1. GET `/api/v1/admin/exercises/{id}` | exerciseId = 1 | HTTP 200, trả về ExerciseDto | | Pending | Happy path |
| TC-ADMIN-012 | Lấy exercise không tồn tại | 1. GET với id = 99 | id không có | HTTP 404, message: "Exercise with ID 99 not found" | | Pending | Not found |
| TC-ADMIN-013 | Lấy danh sách exercises với filter | 1. GET `/api/v1/admin/exercises?muscleGroup=Chest&difficulty=Beginner` | Filter params | HTTP 200, trả về PaginatedResult với exercises matching filters | | Pending | Filtered list |
| TC-ADMIN-014 | Tạo exercise mới | 1. POST `/api/v1/admin/exercises` | `{"name": "Pull Up", "muscleGroup": "Back", "difficulty": "Intermediate", "equipment": "Bar"}` | HTTP 201, CreatedAtAction với ExerciseDto | | Pending | Create exercise |
| TC-ADMIN-015 | Tạo exercise thiếu name | 1. POST với name = null | ModelState invalid | HTTP 400, SerializableError | | Pending | Validation error |
| TC-ADMIN-016 | Cập nhật exercise | 1. PUT `/api/v1/admin/exercises/{id}` | `{"name": "Updated Push Up", "description": "..."}` | HTTP 200, trả về ExerciseDto đã cập nhật | | Pending | Update exercise |
| TC-ADMIN-017 | Xóa exercise không có sessions | 1. DELETE `/api/v1/admin/exercises/{id}` | Exercise không được sử dụng | HTTP 204 NoContent | | Pending | Delete success |
| TC-ADMIN-018 | Xóa exercise đang được sử dụng | 1. DELETE exercise có ExerciseSessions | Exercise đang in use | HTTP 409 Conflict, message: "Exercise is in use" | | Pending | Delete conflict |
| TC-ADMIN-019 | Upload ảnh cho exercise | 1. POST `/api/v1/admin/exercises/{id}/image` | File ảnh hợp lệ | HTTP 200, ExerciseDto với ImageUrl mới | | Pending | Upload image |
| TC-ADMIN-020 | Upload ảnh không có file | 1. POST image không gửi file | file = null | HTTP 400, "No file uploaded" | | Pending | No file |

### 9.4 Quản lý thư viện món ăn (UC-A04)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-ADMIN-021 | Lấy danh sách food items | 1. GET `/api/v1/admin/foods?page=1&size=20` | Query params | HTTP 200, PaginatedResult với FoodItemDto | | Pending | Happy path |
| TC-ADMIN-022 | Lấy food items với search | 1. GET với search=chicken, category=Protein | Filter params | HTTP 200, filtered results | | Pending | Search filter |
| TC-ADMIN-023 | Lấy food item theo ID | 1. GET `/api/v1/admin/foods/{id}` | foodItemId = 1 | HTTP 200, FoodItemDto với đầy đủ nutritional info | | Pending | Get by ID |
| TC-ADMIN-024 | Lấy food item không tồn tại | 1. GET với id = 999 | id không có | HTTP 404, message về food item not found | | Pending | Not found |
| TC-ADMIN-025 | Tạo food item mới | 1. POST `/api/v1/admin/foods` | `{"name": "Chicken Breast", "category": "Protein", "servingSize": 100, "servingUnit": "g", "caloriesPerServing": 165, "proteinG": 31, "carbsG": 0, "fatG": 3.6}` | HTTP 201, CreatedAtAction với FoodItemDto | | Pending | Create food |
| TC-ADMIN-026 | Tạo food item thiếu name | 1. POST với ModelState invalid | Thiếu required fields | HTTP 400, SerializableError | | Pending | Validation error |
| TC-ADMIN-027 | Cập nhật food item | 1. PUT `/api/v1/admin/foods/{id}` | Updated nutritional data | HTTP 200, FoodItemDto đã cập nhật | | Pending | Update food |
| TC-ADMIN-028 | Service exception khi tạo food | 1. POST nhưng database lỗi | Service throws Exception | HTTP 500, "An error occurred while creating the food item" | | Pending | Service error |

### 9.5 Quản lý thử thách - Admin (UC-A05)

| Test Case ID | Mô tả | Bước kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Ghi chú |
|--------------|-------|---------------|-----------------|------------------|-----------------|------------|---------|
| TC-ADMIN-029 | Tạo challenge mới | 1. POST `/api/v1/admin/challenges` | `{"title": "30-Day Running", "description": "...", "challengeType": "Workout", "startDate": "...", "endDate": "...", "criteria": "...", "maxParticipants": 100}` | HTTP 201, Challenge được tạo với status=Open | | Pending | Create challenge |
| TC-ADMIN-030 | Lấy danh sách participants của challenge | 1. GET `/api/v1/admin/challenges/{id}/participants?status=PendingApproval` | Filter by status | HTTP 200, danh sách participations cần duyệt | | Pending | List participants |
| TC-ADMIN-031 | Duyệt submission - Approve | 1. PUT `/api/v1/admin/challenges/participations/{id}/review` | `{"approved": true, "reviewNotes": "Great job!"}` | HTTP 200, status chuyển thành Completed, cập nhật completedAt, reviewedByAdminId | | Pending | Approve submission |
| TC-ADMIN-032 | Duyệt submission - Reject | 1. PUT review với approved = false | `{"approved": false, "reviewNotes": "Not enough evidence"}` | HTTP 200, status chuyển thành Failed | | Pending | Reject submission |
| TC-ADMIN-033 | Đóng challenge | 1. PUT `/api/v1/admin/challenges/{id}` với status=Closed | Update status | HTTP 200, Challenge.status = Closed, không cho join thêm | | Pending | Close challenge |

---

## TỔNG HỢP THỐNG KÊ TEST CASES

| Module | Số lượng Test Cases | Ưu tiên |
|--------|---------------------|---------|
| Authentication | 14 | Cao |
| User Management | 13 | Cao |
| Goal Management | 14 | Trung bình |
| Workout Logs | 11 | Trung bình |
| Nutrition Logs | 11 | Trung bình |
| Challenges | 13 | Trung bình |
| Forum | 11 | Thấp |
| Leaderboard | 10 | Thấp |
| Admin | 33 | Cao |
| **TỔNG** | **130** | - |

---

## GHI CHÚ CHUNG

### Môi trường kiểm thử
- **API Base URL**: https://localhost:7144 (Development) hoặc Production URL
- **Database**: SQL Server với test data đã seed
- **Authentication**: JWT Bearer tokens
- **Swagger UI**: https://localhost:7144/swagger

### Công cụ kiểm thử
- **Manual Testing**: Postman, Swagger UI
- **Automated Testing**: xUnit, FluentAssertions (đã có Unit Tests)
- **Performance Testing**: k6, JMeter (nếu cần)

### Tiêu chí Pass/Fail
- **Pass**: Kết quả thực tế khớp với kết quả mong đợi
- **Fail**: Kết quả thực tế khác với kết quả mong đợi
- **Blocked**: Không thể thực hiện do lỗi dependencies

### Liên hệ
- **Developer Team**: HealthSync Team
- **Tester**: [Tên người test]
- **Ngày bắt đầu test**: [DD/MM/YYYY]
- **Ngày kết thúc test dự kiến**: [DD/MM/YYYY]
