# Get Cloudflare Quick Tunnel URLs
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("prod", "dev", "all")]
    [string]$Environment = "all"
)

function Get-TunnelUrl {
    param(
        [string]$ContainerName,
        [string]$ServiceName
    )
    
    Write-Host "`n$ServiceName" -ForegroundColor Yellow
    
    $isRunning = docker ps --filter "name=$ContainerName" --format "{{.Names}}" 2>$null
    
    if (-not $isRunning) {
        Write-Host "  Container not running" -ForegroundColor Red
        return $null
    }
    
    $url = docker logs $ContainerName 2>&1 | Select-String "https://.*\.trycloudflare\.com" | Select-Object -First 1
    
    if ($url) {
        $urlMatch = [regex]::Match($url.ToString(), "https://[^\s]+\.trycloudflare\.com")
        if ($urlMatch.Success) {
            Write-Host "  $($urlMatch.Value)" -ForegroundColor Green
            return $urlMatch.Value
        }
    }
    
    Write-Host "  URL not found yet. Container may be starting..." -ForegroundColor DarkYellow
    return $null
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Cloudflare Quick Tunnel URLs" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($Environment -eq "prod" -or $Environment -eq "all") {
    Write-Host "`nPRODUCTION:" -ForegroundColor Green
    
    $apiUrl = Get-TunnelUrl "healthsync-tunnel-nginx" "API (nginx)"
    $minioUrl = Get-TunnelUrl "healthsync-tunnel-minio" "MinIO Files"
    $consoleUrl = Get-TunnelUrl "healthsync-tunnel-minio-console" "MinIO Console"
    
    if ($apiUrl) {
        Write-Host "`nTesting API health..." -ForegroundColor Cyan
        try {
            $response = Invoke-WebRequest -Uri "$apiUrl/health" -Method GET -TimeoutSec 5 -UseBasicParsing
            Write-Host "API is responding (Status: $($response.StatusCode))" -ForegroundColor Green
        } catch {
            Write-Host "API not responding yet" -ForegroundColor DarkYellow
        }
    }
}

if ($Environment -eq "dev" -or $Environment -eq "all") {
    Write-Host "`nDEVELOPMENT:" -ForegroundColor Blue
    
    $jenkinsUrl = Get-TunnelUrl "healthsync-tunnel-jenkins" "Jenkins"
    $sonarUrl = Get-TunnelUrl "healthsync-tunnel-sonarqube" "SonarQube"
}

Write-Host "`n========================================`n" -ForegroundColor Cyan
