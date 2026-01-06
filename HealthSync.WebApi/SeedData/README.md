# Seed Data Images

This folder contains sample images for database seeding in Production/Staging environments.

## Folder Structure

```
images/
├── avatars/
│   ├── avatar_01.jpg
│   ├── avatar_02.jpg
│   └── ...
├── exercises/
│   ├── bench_press.jpg
│   ├── squat.jpg
│   └── ...
├── foods/
│   ├── chicken_breast.jpg
│   ├── white_rice.jpg
│   └── ...
└── forum-posts/
    ├── post_transform.jpg
    ├── post_meal.jpg
    └── ...
```

## Image Requirements

### Avatars (10 images recommended)
- **Files**: `avatar_01.jpg` through `avatar_10.jpg`
- **Size**: 200x200px minimum, 500x500px recommended
- **Format**: JPEG or PNG
- **Max file size**: 200KB each

### Exercise Images (matches ExerciseCatalog)
- **Naming**: Use snake_case matching `ImageFileName` in `ExerciseCatalog.cs`
- **Size**: 400x300px recommended
- **Format**: JPEG
- **Max file size**: 150KB each

### Food Images (matches FoodItemCatalog)
- **Naming**: Use snake_case matching `ImageFileName` in `FoodItemCatalog.cs`
- **Size**: 400x300px recommended
- **Format**: JPEG
- **Max file size**: 150KB each

### Forum Post Images
- **Files**: `post_transform.jpg`, `post_meal.jpg`, `post_gym.jpg`, `post_progress.jpg`
- **Size**: 800x600px recommended
- **Format**: JPEG
- **Max file size**: 300KB each

## CI/CD Integration

These images are copied into the Docker image during build:

```dockerfile
COPY ["HealthSync.WebApi/SeedData/", "/app/seed-data/"]
```

The seeder reads from `/app/seed-data/images/` inside the container.

## Configuration

Control seeding via `appsettings.json` or environment variables:

```json
{
  "SeedSettings": {
    "EnableDataSeeding": true,
    "SeedDemoData": true,
    "DemoCustomerCount": 20,
    "ActivityLogDays": 30,
    "SeedImagePath": "/app/seed-data/images",
    "DefaultCustomerPassword": "Demo@123456"
  }
}
```

Or via environment variables:
```bash
SeedSettings__EnableDataSeeding=true
SeedSettings__SeedDemoData=true
```

## Notes

1. Images are uploaded to MinIO bucket during seeding
2. Seeding is idempotent - running multiple times won't duplicate data
3. If an image file is missing, seeding continues with null ImageUrl
4. Demo customer password should be changed in production
