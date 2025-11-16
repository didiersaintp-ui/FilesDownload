#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validate the entire project setup

.DESCRIPTION
    Checks that all prerequisites are installed and the project is ready to use

.EXAMPLE
    .\validate.ps1
#>

$ErrorActionPreference = 'Stop'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Project Validation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$validationResults = @()

function Test-Command {
    param([string]$Command)
    return $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

function Add-ValidationResult {
    param(
        [string]$Name,
        [bool]$Success,
        [string]$Message = ""
    )

    $script:validationResults += [PSCustomObject]@{
        Name    = $Name
        Success = $Success
        Message = $Message
    }
}

# Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
if (Test-Command dotnet) {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -match '^8\.') {
        Add-ValidationResult -Name ".NET 8 SDK" -Success $true -Message "Version: $dotnetVersion"
        Write-Host "✓ .NET 8 SDK found: $dotnetVersion" -ForegroundColor Green
    }
    else {
        Add-ValidationResult -Name ".NET 8 SDK" -Success $false -Message "Found version $dotnetVersion, but .NET 8 is required"
        Write-Host "✗ .NET 8 SDK not found (found $dotnetVersion)" -ForegroundColor Red
    }
}
else {
    Add-ValidationResult -Name ".NET 8 SDK" -Success $false -Message "Not installed"
    Write-Host "✗ .NET SDK not found" -ForegroundColor Red
}

# Check Docker
Write-Host "Checking Docker..." -ForegroundColor Yellow
if (Test-Command docker) {
    $dockerVersion = docker --version
    Add-ValidationResult -Name "Docker" -Success $true -Message $dockerVersion
    Write-Host "✓ Docker found: $dockerVersion" -ForegroundColor Green
}
else {
    Add-ValidationResult -Name "Docker" -Success $false -Message "Not installed"
    Write-Host "✗ Docker not found" -ForegroundColor Red
}

# Check Azure CLI
Write-Host "Checking Azure CLI..." -ForegroundColor Yellow
if (Test-Command az) {
    $azVersion = az version --query '\"azure-cli\"' -o tsv
    Add-ValidationResult -Name "Azure CLI" -Success $true -Message "Version: $azVersion"
    Write-Host "✓ Azure CLI found: $azVersion" -ForegroundColor Green
}
else {
    Add-ValidationResult -Name "Azure CLI" -Success $false -Message "Not installed"
    Write-Host "✗ Azure CLI not found" -ForegroundColor Red
}

# Check Terraform
Write-Host "Checking Terraform..." -ForegroundColor Yellow
if (Test-Command terraform) {
    $tfVersion = terraform version -json | ConvertFrom-Json | Select-Object -ExpandProperty terraform_version
    Add-ValidationResult -Name "Terraform" -Success $true -Message "Version: $tfVersion"
    Write-Host "✓ Terraform found: $tfVersion" -ForegroundColor Green
}
else {
    Add-ValidationResult -Name "Terraform" -Success $false -Message "Not installed"
    Write-Host "✗ Terraform not found" -ForegroundColor Red
}

# Check Helm
Write-Host "Checking Helm..." -ForegroundColor Yellow
if (Test-Command helm) {
    $helmVersion = helm version --short
    Add-ValidationResult -Name "Helm" -Success $true -Message $helmVersion
    Write-Host "✓ Helm found: $helmVersion" -ForegroundColor Green
}
else {
    Add-ValidationResult -Name "Helm" -Success $false -Message "Not installed"
    Write-Host "✗ Helm not found" -ForegroundColor Red
}

# Check kubectl
Write-Host "Checking kubectl..." -ForegroundColor Yellow
if (Test-Command kubectl) {
    $kubectlVersion = kubectl version --client --short 2>$null
    Add-ValidationResult -Name "kubectl" -Success $true -Message $kubectlVersion
    Write-Host "✓ kubectl found: $kubectlVersion" -ForegroundColor Green
}
else {
    Add-ValidationResult -Name "kubectl" -Success $false -Message "Not installed"
    Write-Host "✗ kubectl not found" -ForegroundColor Red
}

# Check project structure
Write-Host ""
Write-Host "Checking project structure..." -ForegroundColor Yellow

$requiredPaths = @(
    "shared/models",
    "solution-a-blob/src/API",
    "solution-b-minio/src/API",
    "device-simulator/src",
    "infrastructure/terraform",
    "scripts"
)

$rootPath = Split-Path -Parent $PSScriptRoot

foreach ($path in $requiredPaths) {
    $fullPath = Join-Path $rootPath $path
    if (Test-Path $fullPath) {
        Write-Host "✓ $path" -ForegroundColor Green
    }
    else {
        Write-Host "✗ $path missing" -ForegroundColor Red
        Add-ValidationResult -Name "Project structure" -Success $false -Message "$path missing"
    }
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Validation Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$passedCount = ($validationResults | Where-Object { $_.Success }).Count
$failedCount = ($validationResults | Where-Object { -not $_.Success }).Count

Write-Host ""
Write-Host "Passed: $passedCount" -ForegroundColor Green
Write-Host "Failed: $failedCount" -ForegroundColor Red

if ($failedCount -gt 0) {
    Write-Host ""
    Write-Host "Failed validations:" -ForegroundColor Red
    $validationResults | Where-Object { -not $_.Success } | ForEach-Object {
        Write-Host "  - $($_.Name): $($_.Message)" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "Please install missing prerequisites before proceeding." -ForegroundColor Yellow
    Write-Host "See README.md for installation instructions." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "✓ All validations passed!" -ForegroundColor Green
Write-Host "You're ready to build and deploy the project." -ForegroundColor Green
