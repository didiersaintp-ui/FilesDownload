#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build all .NET projects

.DESCRIPTION
    Builds shared models, Solution A API, Solution B API, and Device Simulator

.PARAMETER Configuration
    Build configuration (Debug or Release)

.EXAMPLE
    .\build.ps1
    .\build.ps1 -Configuration Release
#>

param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Device Manifest POC Projects" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$rootPath = Split-Path -Parent $PSScriptRoot

# Build shared models
Write-Host "Building shared models..." -ForegroundColor Yellow
Push-Location "$rootPath/shared/models"
try {
    dotnet build DeviceManifest.Shared.csproj -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Failed to build shared models" }
}
finally {
    Pop-Location
}
Write-Host "✓ Shared models built successfully" -ForegroundColor Green
Write-Host ""

# Build Solution A API
Write-Host "Building Solution A API (Azure Blob)..." -ForegroundColor Yellow
Push-Location "$rootPath/solution-a-blob/src/API"
try {
    dotnet build DeviceManifest.Api.csproj -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Failed to build Solution A API" }
}
finally {
    Pop-Location
}
Write-Host "✓ Solution A API built successfully" -ForegroundColor Green
Write-Host ""

# Build Solution B API
Write-Host "Building Solution B API (MinIO)..." -ForegroundColor Yellow
Push-Location "$rootPath/solution-b-minio/src/API"
try {
    dotnet build DeviceManifest.Api.Minio.csproj -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Failed to build Solution B API" }
}
finally {
    Pop-Location
}
Write-Host "✓ Solution B API built successfully" -ForegroundColor Green
Write-Host ""

# Build Device Simulator
Write-Host "Building Device Simulator..." -ForegroundColor Yellow
Push-Location "$rootPath/device-simulator/src"
try {
    dotnet build DeviceSimulator.csproj -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Failed to build Device Simulator" }
}
finally {
    Pop-Location
}
Write-Host "✓ Device Simulator built successfully" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✓ All projects built successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
