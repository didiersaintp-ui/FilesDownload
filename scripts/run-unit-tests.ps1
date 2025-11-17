#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run all unit tests

.DESCRIPTION
    Runs unit tests for all projects

.EXAMPLE
    .\run-unit-tests.ps1
#>

$ErrorActionPreference = 'Stop'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running Unit Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$rootPath = Split-Path -Parent $PSScriptRoot

# Test projects
$testProjects = @(
    "$rootPath/solution-a-blob/tests/DeviceManifest.Api.Tests.csproj",
    "$rootPath/solution-b-minio/tests/DeviceManifest.Api.Minio.Tests.csproj",
    "$rootPath/device-simulator/tests/DeviceSimulator.Tests.csproj"
)

$totalTests = 0
$passedTests = 0
$failedTests = 0

foreach ($project in $testProjects) {
    $projectName = Split-Path (Split-Path $project) -Leaf
    Write-Host "Running tests for $projectName..." -ForegroundColor Yellow

    dotnet test $project --verbosity minimal

    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Tests passed for $projectName" -ForegroundColor Green
        $passedTests++
    }
    else {
        Write-Host "[FAIL] Tests failed for $projectName" -ForegroundColor Red
        $failedTests++
    }

    Write-Host ""
    $totalTests++
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total projects: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red

if ($failedTests -gt 0) {
    exit 1
}

Write-Host ""
Write-Host "[OK] All tests passed!" -ForegroundColor Green
