# Prod Environment Management Script
# Usage: .\prod.ps1 [up|down|restart|clean|logs]

param(
    [Parameter(Mandatory=$false)]
    [string]$Action = "up"
)

$envFile = ".env.prod"
$composeFile = "docker-compose.prod.yml"

switch ($Action) {
    "up" {
        Write-Host "Starting production environment..." -ForegroundColor Green
        & docker-compose --env-file $envFile -f $composeFile up -d --remove-orphans
        Write-Host "Services started" -ForegroundColor Green
    }
    "down" {
        Write-Host "Stopping production environment..." -ForegroundColor Yellow
        & docker-compose --env-file $envFile -f $composeFile down --remove-orphans
        Write-Host "Services stopped" -ForegroundColor Green
    }
    "restart" {
        Write-Host "Restarting production environment..." -ForegroundColor Cyan
        & docker-compose --env-file $envFile -f $composeFile restart
        Write-Host "Services restarted" -ForegroundColor Green
    }
    "clean" {
        Write-Host "Cleaning up all healthsync containers..." -ForegroundColor Red
        & docker rm -f $(docker ps -aq --filter "name=healthsync") 2>$null
        Write-Host "Clean up completed." -ForegroundColor Green
    }
    "logs" {
        Write-Host "Showing logs (Ctrl+C to exit)..." -ForegroundColor Cyan
        & docker-compose --env-file $envFile -f $composeFile logs -f
    }
    default {
        Write-Host "Usage: .\prod.ps1 [up|down|restart|clean|logs]" -ForegroundColor White
        Write-Host ""
        Write-Host "Commands:" -ForegroundColor Cyan
        Write-Host "  up      - Start all services" -ForegroundColor White
        Write-Host "  down    - Stop all services" -ForegroundColor White
        Write-Host "  restart - Restart all services" -ForegroundColor White
        Write-Host "  clean   - Remove all containers" -ForegroundColor White
        Write-Host "  logs    - View logs" -ForegroundColor White
    }
}
