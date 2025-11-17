#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run device simulator tests against an API

.DESCRIPTION
    Runs the device simulator with configurable parameters

.PARAMETER ApiUrl
    API base URL (e.g., http://localhost:5000)

.PARAMETER DeviceCount
    Number of devices to simulate (default: 100)

.PARAMETER Parallelism
    Max parallel requests (default: 10)

.PARAMETER DownloadFiles
    Actually download files (default: false)

.PARAMETER OutputFile
    Output file for metrics (JSON)

.EXAMPLE
    .\run-tests.ps1 -ApiUrl "http://10.0.1.100" -DeviceCount 100
    .\run-tests.ps1 -ApiUrl "http://10.0.1.100" -DeviceCount 1000 -Parallelism 50 -DownloadFiles -OutputFile "metrics.json"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ApiUrl,

    [Parameter()]
    [int]$DeviceCount = 100,

    [Parameter()]
    [int]$Parallelism = 10,

    [Parameter()]
    [switch]$DownloadFiles,

    [Parameter()]
    [string]$OutputFile
)

$ErrorActionPreference = 'Stop'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running Device Simulator Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "API URL: $ApiUrl" -ForegroundColor Yellow
Write-Host "Device Count: $DeviceCount" -ForegroundColor Yellow
Write-Host "Parallelism: $Parallelism" -ForegroundColor Yellow
Write-Host "Download Files: $DownloadFiles" -ForegroundColor Yellow
if ($OutputFile) {
    Write-Host "Output File: $OutputFile" -ForegroundColor Yellow
}
Write-Host ""

$rootPath = Split-Path -Parent $PSScriptRoot
$simulatorPath = "$rootPath/device-simulator/src"

# Build simulator if needed
if (-not (Test-Path "$simulatorPath/bin/Release/net8.0/DeviceSimulator.dll")) {
    Write-Host "Building Device Simulator..." -ForegroundColor Yellow
    Push-Location $simulatorPath
    try {
        dotnet build -c Release
        if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    }
    finally {
        Pop-Location
    }
    Write-Host "[OK] Build complete" -ForegroundColor Green
    Write-Host ""
}

# Run simulator
Write-Host "Starting simulation..." -ForegroundColor Yellow
Write-Host ""

Push-Location $simulatorPath
try {
    $args = @(
        "run",
        "--no-build",
        "-c", "Release",
        "--",
        "--api-url", $ApiUrl,
        "--device-count", $DeviceCount,
        "--parallelism", $Parallelism
    )

    if ($DownloadFiles) {
        $args += "--download-files"
    }

    if ($OutputFile) {
        $args += "--output"
        $args += $OutputFile
    }

    & dotnet $args

    if ($LASTEXITCODE -ne 0) { throw "Simulation failed" }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[OK] Simulation complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

if ($OutputFile -and (Test-Path $OutputFile)) {
    Write-Host ""
    Write-Host "Metrics saved to: $OutputFile" -ForegroundColor Green
}
