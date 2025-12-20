#!/bin/bash
set -e

echo "=========================================="
echo "HealthSync Migration Init Container"
echo "=========================================="

# Extract DB host from connection string
DB_HOST=$(echo "$ConnectionStrings__DefaultConnection" | grep -oP '(?<=Server=)[^;]+' || echo "db")
DB_PORT=1433

echo "Waiting for SQL Server at ${DB_HOST}:${DB_PORT}..."

# Wait for database (max 120 seconds - increased for slow servers)
MAX_RETRIES=120
RETRY_COUNT=0

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if nc -z -w 3 "$DB_HOST" "$DB_PORT" 2>/dev/null; then
        echo "✓ SQL Server is ready!"
        break
    fi

    RETRY_COUNT=$((RETRY_COUNT + 1))
    echo "Waiting... (attempt $RETRY_COUNT/$MAX_RETRIES)"
    sleep 2
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo "✗ ERROR: SQL Server not ready after ${MAX_RETRIES} attempts"
    exit 1
fi

echo ""
echo "Running EF Core migrations..."
echo "Connection: ${ConnectionStrings__DefaultConnection}"
echo ""

# Execute EF Core Bundle
/app/efbundle --connection "$ConnectionStrings__DefaultConnection" --verbose

if [ $? -eq 0 ]; then
    echo ""
    echo "=========================================="
    echo "✓ Migrations completed successfully!"
    echo "=========================================="
    exit 0
else
    echo ""
    echo "=========================================="
    echo "✗ Migration failed!"
    echo "=========================================="
    exit 1
fi