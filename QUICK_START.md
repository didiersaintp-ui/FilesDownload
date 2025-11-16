# Quick Start Guide

Guide de démarrage rapide pour lancer le POC en 15 minutes.

## Prérequis

Sur Windows, installez :
1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. [Docker Desktop](https://www.docker.com/products/docker-desktop)
3. [Azure CLI](https://aka.ms/azure-cli)
4. [Terraform](https://www.terraform.io/downloads)
5. [Helm](https://helm.sh/docs/intro/install/)
6. [kubectl](https://kubernetes.io/docs/tasks/tools/)

## Étapes rapides

### 1. Validation

```powershell
.\scripts\validate.ps1
```

Si des outils manquent, installez-les avant de continuer.

### 2. Build

```powershell
.\scripts\build.ps1
```

### 3. Login Azure

```powershell
az login
az account set --subscription "<your-subscription-id>"
```

### 4. Déployer l'infrastructure

```powershell
# Choisissez un nom unique pour le Storage Account (3-24 caractères alphanumériques minuscules)
$storageAccountName = "stdevmanifest$(Get-Random -Maximum 9999)"

.\scripts\deploy-infrastructure.ps1 -StorageAccountName $storageAccountName
```

⏱️ **Durée : ~20 minutes**

### 5. Créer un Azure Container Registry

```powershell
$acrName = "acr$(Get-Random -Maximum 99999)"

az acr create `
    --resource-group rg-devicemanifest-aks `
    --name $acrName `
    --sku Standard
```

### 6. Déployer Solution A

```powershell
.\scripts\deploy-solution-a.ps1 `
    -AcrName $acrName `
    -ClusterName "aks-devicemanifest" `
    -ResourceGroup "rg-devicemanifest-aks" `
    -StorageAccountName $storageAccountName
```

⏱️ **Durée : ~10 minutes**

### 7. Uploader des fichiers de test

```powershell
# Créer des fichiers de test
New-Item -Path "test-files" -ItemType Directory -Force
1..20 | ForEach-Object {
    $size = Get-Random -Minimum 1KB -Maximum 10MB
    $bytes = New-Object byte[] $size
    (New-Object Random).NextBytes($bytes)
    [IO.File]::WriteAllBytes("test-files/file-$_.bin", $bytes)
}

# Uploader vers Azure Blob
az storage blob upload-batch `
    --account-name $storageAccountName `
    --destination device-files `
    --source ./test-files `
    --auth-mode login
```

### 8. Tester

```powershell
# Obtenir l'IP du service
$apiUrl = kubectl get svc devicemanifest-api-blob -o jsonpath='{.status.loadBalancer.ingress[0].ip}'

# Attendre que l'IP soit assignée
while ([string]::IsNullOrEmpty($apiUrl)) {
    Start-Sleep -Seconds 5
    $apiUrl = kubectl get svc devicemanifest-api-blob -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
}

Write-Host "API URL: http://$apiUrl"

# Tester l'API
curl "http://$apiUrl/api/manifest/CEB0000000000000001"

# Lancer le simulateur
.\scripts\run-tests.ps1 `
    -ApiUrl "http://$apiUrl" `
    -DeviceCount 100 `
    -Parallelism 10 `
    -DownloadFiles `
    -OutputFile "metrics-solution-a.json"
```

## Résultat attendu

Le simulateur devrait afficher :
- Nombre de devices simulés
- Nombre de fichiers téléchargés
- Total de bytes transférés
- Latence moyenne
- Taux d'erreur

Les métriques sont sauvegardées dans `metrics-solution-a.json`.

## Déployer Solution B (optionnel)

```powershell
.\scripts\deploy-solution-b.ps1 `
    -AcrName $acrName `
    -ClusterName "aks-devicemanifest" `
    -ResourceGroup "rg-devicemanifest-aks"

# Attendre que MinIO soit prêt
kubectl wait --for=condition=ready pod -l app=minio --timeout=300s

# Port-forward MinIO console
kubectl port-forward svc/devicemanifest-solution-b-minio-console 9001:9001

# Ouvrir http://localhost:9001
# Login: minioadmin / minioadmin123
# Uploader les fichiers de test dans le bucket "device-files"

# Tester Solution B
$apiUrlB = kubectl get svc devicemanifest-solution-b-api -o jsonpath='{.status.loadBalancer.ingress[0].ip}'

.\scripts\run-tests.ps1 `
    -ApiUrl "http://$apiUrlB" `
    -DeviceCount 100 `
    -Parallelism 10 `
    -DownloadFiles `
    -OutputFile "metrics-solution-b.json"
```

## Comparer les résultats

```powershell
# Analyser les métriques
$metricsA = Get-Content "metrics-solution-a.json" | ConvertFrom-Json
$metricsB = Get-Content "metrics-solution-b.json" | ConvertFrom-Json

Write-Host "========== COMPARISON ==========" -ForegroundColor Cyan
Write-Host ""
Write-Host "Solution A (Azure Blob):" -ForegroundColor Yellow
Write-Host "  Total bytes: $($metricsA.TotalBytesDownloaded / 1MB) MB"
Write-Host "  Duration: $($metricsA.TotalDurationSeconds) seconds"
Write-Host "  Avg speed: $(($metricsA.TotalBytesDownloaded / 1MB) / $metricsA.TotalDurationSeconds) MB/s"
Write-Host ""
Write-Host "Solution B (MinIO):" -ForegroundColor Yellow
Write-Host "  Total bytes: $($metricsB.TotalBytesDownloaded / 1MB) MB"
Write-Host "  Duration: $($metricsB.TotalDurationSeconds) seconds"
Write-Host "  Avg speed: $(($metricsB.TotalBytesDownloaded / 1MB) / $metricsB.TotalDurationSeconds) MB/s"
```

## Monitoring

### Logs des APIs

```powershell
# Solution A
kubectl logs -l app.kubernetes.io/name=devicemanifest-api-blob --tail=100 -f

# Solution B
kubectl logs -l app.kubernetes.io/component=api --tail=100 -f
```

### Métriques des pods

```powershell
kubectl top pods
```

### Azure Cost Management

```powershell
# Ouvrir Azure Portal > Cost Management
# Analyser les coûts par resource group et service
```

## Nettoyage

```powershell
# Supprimer les déploiements Helm
helm uninstall devicemanifest-api-blob
helm uninstall devicemanifest-solution-b

# Détruire l'infrastructure Terraform
cd infrastructure/terraform
terraform destroy

# Supprimer l'ACR
az acr delete --name $acrName --yes
```

## Troubleshooting

### "No space left on device" lors du build Docker

Nettoyez Docker :
```powershell
docker system prune -a
```

### Terraform timeout

Augmentez le timeout dans les modules Terraform ou relancez `terraform apply`.

### Pods en CrashLoopBackOff

Vérifiez les logs :
```powershell
kubectl describe pod <pod-name>
kubectl logs <pod-name>
```

### Performance lente

- Augmentez le nombre de replicas
- Utilisez des VMs plus grandes pour AKS
- Vérifiez la latence réseau vers le Storage

## Support

Consultez le README.md complet pour plus de détails sur chaque étape.
