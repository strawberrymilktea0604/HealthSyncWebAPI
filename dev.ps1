# Dev Environment Management Script
# Usage: .\dev.ps1 [up|down|restart|clean]

param(
    [Parameter(Mandatory=$false)]
    [string]$Action = "up"
)

$envFile = ".env.dev"
$composeFile = "docker-compose.dev.yml"

switch ($Action) {
    "up" {
        Write-Host "Starting development environment..." -ForegroundColor Green
        & docker-compose --env-file $envFile -f $composeFile up -d
    }
    "down" {
        Write-Host "Stopping development environment..." -ForegroundColor Yellow
        & docker-compose --env-file $envFile -f $composeFile down --remove-orphans
    }
    "restart" {
        Write-Host "Restarting development environment..." -ForegroundColor Cyan
        & docker-compose --env-file $envFile -f $composeFile down --remove-orphans
        Start-Sleep -Seconds 2
        & docker-compose --env-file $envFile -f $composeFile up -d
    }
    "clean" {
        Write-Host "Cleaning up all healthsync containers..." -ForegroundColor Red
        & docker rm -f $(docker ps -aq --filter "name=healthsync") 2>$null
        & docker volume rm $(docker volume ls -q --filter "name=healthsync") 2>$null
        Write-Host "Clean up completed." -ForegroundColor Green
    }
    default {
        Write-Host "Usage: .\dev.ps1 [up|down|restart|clean]" -ForegroundColor White
        Write-Host "  up      - Start all services" -ForegroundColor White
        Write-Host "  down    - Stop all services" -ForegroundColor White
        Write-Host "  restart - Restart all services" -ForegroundColor White
        Write-Host "  clean   - Remove all containers and volumes" -ForegroundColor White
    }
}