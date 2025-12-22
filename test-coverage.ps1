#!/usr/bin/env pwsh
# Script tu dong chay tests voi coverage va generate reports (loai bo Program.cs)

param(
    [switch]$SkipTests,
    [switch]$HtmlOnly,
    [switch]$TextOnly
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "HealthSync - Test Coverage Report" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Clean old test results
if (Test-Path "TestResults") {
    Write-Host "`n[1/4] Cleaning old test results..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force TestResults
    Write-Host "[OK] Cleaned TestResults directory" -ForegroundColor Green
} else {
    Write-Host "`n[1/4] No old test results to clean" -ForegroundColor Gray
}

# 2. Run tests with coverage
if (-not $SkipTests) {
    Write-Host "`n[2/4] Running tests with coverage collection..." -ForegroundColor Yellow
    dotnet test --settings HealthSync.runsettings `
                --collect:"XPlat Code Coverage" `
                --results-directory TestResults `
                --logger "console;verbosity=minimal"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[X] Tests failed or had errors (exit code: $LASTEXITCODE)" -ForegroundColor Red
        Write-Host "  Continuing to generate coverage report..." -ForegroundColor Yellow
    } else {
        Write-Host "[OK] All tests passed!" -ForegroundColor Green
    }
} else {
    Write-Host "`n[2/4] Skipping tests (using existing coverage data)" -ForegroundColor Gray
}

# 3. Generate reports
Write-Host "`n[3/4] Generating coverage reports..." -ForegroundColor Yellow

$reportGenerated = $false

if (-not $TextOnly) {
    Write-Host "  -> Generating HTML report..." -ForegroundColor Cyan
    reportgenerator -reports:"TestResults/**/coverage.opencover.xml" `
                    -targetdir:"coveragereport" `
                    -reporttypes:Html `
                    -classfilters:"-Program;-*DTO*;-*Configuration*;-*Migration*" `
                    -assemblyfilters:"+HealthSync.*"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] HTML report: coveragereport\index.html" -ForegroundColor Green
        $reportGenerated = $true
    } else {
        Write-Host "  [X] Failed to generate HTML report" -ForegroundColor Red
    }
}

if (-not $HtmlOnly) {
    Write-Host "  -> Generating text summary..." -ForegroundColor Cyan
    reportgenerator -reports:"TestResults/**/coverage.opencover.xml" `
                    -targetdir:"." `
                    -reporttypes:TextSummary `
                    -classfilters:"-Program;-*DTO*;-*Configuration*;-*Migration*" `
                    -assemblyfilters:"+HealthSync.*"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] Text summary: Summary.txt" -ForegroundColor Green
        $reportGenerated = $true
    } else {
        Write-Host "  [X] Failed to generate text summary" -ForegroundColor Red
    }
}

# 4. Display summary
if ($reportGenerated) {
    Write-Host "`n[4/4] Coverage Summary:" -ForegroundColor Yellow
    Write-Host "----------------------------------------" -ForegroundColor Gray
    
    if (Test-Path "Summary.txt") {
        $summaryLines = Get-Content "Summary.txt" -TotalCount 20
        foreach ($line in $summaryLines) {
            if ($line -match "Line coverage:") {
                Write-Host $line -ForegroundColor Cyan
            } elseif ($line -match "Branch coverage:") {
                Write-Host $line -ForegroundColor Cyan
            } elseif ($line -match "Method coverage:") {
                Write-Host $line -ForegroundColor Cyan
            } elseif ($line -match "Coverage date:") {
                Write-Host $line -ForegroundColor Gray
            }
        }
    }
    
    Write-Host "----------------------------------------" -ForegroundColor Gray
    Write-Host "`n[OK] Done! Reports generated successfully." -ForegroundColor Green
    Write-Host "  Program.cs excluded from coverage." -ForegroundColor Gray
} else {
    Write-Host "`n[X] Failed to generate reports" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
