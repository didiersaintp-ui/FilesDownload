#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy Azure infrastructure using Terraform

.DESCRIPTION
    Deploys AKS, VNet, Azure Blob Storage, and all supporting infrastructure

.PARAMETER StorageAccountName
    Unique name for the Azure Storage Account (3-24 lowercase alphanumeric)

.PARAMETER Location
    Azure region (default: eastus)

.PARAMETER AutoApprove
    Skip interactive approval of plan

.EXAMPLE
    .\deploy-infrastructure.ps1 -StorageAccountName "stdevicemanifestpoc001"
    .\deploy-infrastructure.ps1 -StorageAccountName "stdevicemanifestpoc001" -Location "westus2" -AutoApprove
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9]{3,24}$')]
    [string]$StorageAccountName,

    [Parameter()]
    [string]$Location = 'eastus',

    [Parameter()]
    [switch]$AutoApprove
)

$ErrorActionPreference = 'Stop'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deploying Azure Infrastructure" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$rootPath = Split-Path -Parent $PSScriptRoot
$terraformPath = "$rootPath/infrastructure/terraform"

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check Azure CLI
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI is not installed. Please install from https://aka.ms/azure-cli"
}

# Check Terraform
if (-not (Get-Command terraform -ErrorAction SilentlyContinue)) {
    throw "Terraform is not installed. Please install from https://www.terraform.io/downloads"
}

# Check Azure login
Write-Host "Checking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in to Azure. Running 'az login'..." -ForegroundColor Yellow
    az login
}
Write-Host "✓ Logged in to Azure as: $($account.user.name)" -ForegroundColor Green
Write-Host "✓ Using subscription: $($account.name)" -ForegroundColor Green
Write-Host ""

# Initialize Terraform
Write-Host "Initializing Terraform..." -ForegroundColor Yellow
Push-Location $terraformPath
try {
    terraform init
    if ($LASTEXITCODE -ne 0) { throw "Terraform init failed" }
    Write-Host "✓ Terraform initialized" -ForegroundColor Green
    Write-Host ""

    # Create terraform.tfvars
    $tfvarsContent = @"
location             = "$Location"
storage_account_name = "$StorageAccountName"

tags = {
  Environment = "POC"
  Project     = "DeviceManifest"
  ManagedBy   = "Terraform"
}
"@

    Set-Content -Path "terraform.tfvars" -Value $tfvarsContent
    Write-Host "✓ Created terraform.tfvars" -ForegroundColor Green
    Write-Host ""

    # Plan
    Write-Host "Creating Terraform plan..." -ForegroundColor Yellow
    terraform plan -out=tfplan
    if ($LASTEXITCODE -ne 0) { throw "Terraform plan failed" }
    Write-Host "✓ Plan created successfully" -ForegroundColor Green
    Write-Host ""

    # Apply
    if ($AutoApprove) {
        Write-Host "Applying Terraform plan (auto-approve)..." -ForegroundColor Yellow
        terraform apply tfplan
    }
    else {
        Write-Host "Review the plan above. Apply? (yes/no): " -ForegroundColor Yellow -NoNewline
        $response = Read-Host
        if ($response -eq 'yes') {
            terraform apply tfplan
        }
        else {
            Write-Host "Deployment cancelled" -ForegroundColor Red
            return
        }
    }

    if ($LASTEXITCODE -ne 0) { throw "Terraform apply failed" }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "✓ Infrastructure deployed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    # Show outputs
    Write-Host "Terraform outputs:" -ForegroundColor Yellow
    terraform output
}
finally {
    Pop-Location
}
