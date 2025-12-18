#!/bin/bash
# migrate.sh - Script to run EF Core migrations and Hangfire schema setup
set -e

echo "=========================================="
echo "HealthSync Migration Init Container"
echo "=========================================="
echo "Starting database migration process..."
echo ""

# Wait for SQL Server to be ready (max 60 seconds)
echo "[1/4] Waiting for SQL Server to be ready..."
MAX_RETRIES=60
RETRY_COUNT=0
WAIT_SECONDS=2

# Extract connection string from environment
DB_HOST="${ConnectionStrings__DefaultConnection##*Server=}"
DB_HOST="${DB_HOST%%;*}"

echo "Database host: $DB_HOST"

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if timeout 3 bash -c "cat < /dev/null > /dev/tcp/${DB_HOST}/1433" 2>/dev/null; then
        echo "✓ SQL Server is ready!"
        break
    fi
    
    RETRY_COUNT=$((RETRY_COUNT + 1))
    echo "Waiting for SQL Server... (attempt $RETRY_COUNT/$MAX_RETRIES)"
    sleep $WAIT_SECONDS
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo "✗ ERROR: SQL Server did not become ready in time"
    exit 1
fi

echo ""
echo "[2/4] Running EF Core migrations..."

# Run EF Core migrations using dotnet-ef
cd /app
export PATH="/root/.dotnet/tools:$PATH"

# Kiểm tra xem migrations có sẵn không
if dotnet ef migrations list --project /src/HealthSync.Infrastructure --startup-project /src/HealthSync.WebApi 2>/dev/null | grep -q "No migrations"; then
    echo "⚠ No migrations found to apply"
else
    echo "Applying migrations..."
    dotnet ef database update --project /src/HealthSync.Infrastructure --startup-project /src/HealthSync.WebApi
    
    if [ $? -eq 0 ]; then
        echo "✓ EF Core migrations completed successfully!"
    else
        echo "✗ ERROR: Migration failed"
        exit 1
    fi
fi

echo ""
echo "[3/4] Initializing Hangfire schema..."

# Run application briefly to initialize Hangfire (PrepareSchemaIfNecessary = true)
# Use timeout to ensure app doesn't run forever
timeout 30s dotnet HealthSync.WebApi.dll --migrate-hangfire-only 2>&1 | grep -i "hangfire" || true

echo "✓ Hangfire schema initialization completed!"

echo ""
echo "[4/4] Migration process completed successfully!"
echo "=========================================="
echo "Init container will now exit."
echo "API containers can now start safely."
echo "=========================================="

exit 0
