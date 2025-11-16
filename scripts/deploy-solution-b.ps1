#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy Solution B (MinIO + API) to AKS

.DESCRIPTION
    Builds Docker image, pushes to ACR, and deploys MinIO + API using Helm

.PARAMETER AcrName
    Azure Container Registry name

.PARAMETER ClusterName
    AKS cluster name

.PARAMETER ResourceGroup
    AKS resource group name

.EXAMPLE
    .\deploy-solution-b.ps1 -AcrName "myacr" -ClusterName "aks-devicemanifest" -ResourceGroup "rg-devicemanifest-aks"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$AcrName,

    [Parameter(Mandatory = $true)]
    [string]$ClusterName,

    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup
)

$ErrorActionPreference = 'Stop'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deploying Solution B (MinIO + API)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$rootPath = Split-Path -Parent $PSScriptRoot

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is not installed"
}

if (-not (Get-Command helm -ErrorAction SilentlyContinue)) {
    throw "Helm is not installed. Please install from https://helm.sh/docs/intro/install/"
}

# Login to ACR
Write-Host "Logging in to ACR..." -ForegroundColor Yellow
az acr login --name $AcrName
if ($LASTEXITCODE -ne 0) { throw "ACR login failed" }
Write-Host "✓ Logged in to ACR" -ForegroundColor Green
Write-Host ""

# Build and push Docker image
$imageName = "$AcrName.azurecr.io/devicemanifest-api-minio"
$imageTag = "latest"

Write-Host "Building Docker image..." -ForegroundColor Yellow
Push-Location $rootPath
try {
    docker build -f solution-b-minio/src/API/Dockerfile -t "${imageName}:${imageTag}" .
    if ($LASTEXITCODE -ne 0) { throw "Docker build failed" }
    Write-Host "✓ Docker image built" -ForegroundColor Green
    Write-Host ""

    Write-Host "Pushing Docker image to ACR..." -ForegroundColor Yellow
    docker push "${imageName}:${imageTag}"
    if ($LASTEXITCODE -ne 0) { throw "Docker push failed" }
    Write-Host "✓ Docker image pushed" -ForegroundColor Green
    Write-Host ""
}
finally {
    Pop-Location
}

# Get AKS credentials
Write-Host "Getting AKS credentials..." -ForegroundColor Yellow
az aks get-credentials --resource-group $ResourceGroup --name $ClusterName --overwrite-existing
if ($LASTEXITCODE -ne 0) { throw "Failed to get AKS credentials" }
Write-Host "✓ AKS credentials configured" -ForegroundColor Green
Write-Host ""

# Add MinIO Helm repo
Write-Host "Adding MinIO Helm repository..." -ForegroundColor Yellow
helm repo add minio https://charts.min.io/
helm repo update
Write-Host "✓ MinIO Helm repository added" -ForegroundColor Green
Write-Host ""

# Deploy with Helm
Write-Host "Deploying with Helm (this may take several minutes)..." -ForegroundColor Yellow
Push-Location "$rootPath/solution-b-minio/deployment/helm"
try {
    # Build dependencies (pulls MinIO chart)
    helm dependency build

    helm upgrade --install devicemanifest-solution-b . `
        --set api.image.repository=$imageName `
        --set api.image.tag=$imageTag `
        --wait --timeout 10m

    if ($LASTEXITCODE -ne 0) { throw "Helm deployment failed" }
    Write-Host "✓ Helm deployment successful" -ForegroundColor Green
    Write-Host ""
}
finally {
    Pop-Location
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✓ Solution B deployed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Checking pod status..." -ForegroundColor Yellow
kubectl get pods -l app.kubernetes.io/instance=devicemanifest-solution-b
Write-Host ""

Write-Host "Services:" -ForegroundColor Yellow
kubectl get svc -l app.kubernetes.io/instance=devicemanifest-solution-b
