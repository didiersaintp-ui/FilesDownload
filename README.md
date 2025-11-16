# Device Manifest POC - Azure Blob vs MinIO

POC comparant deux solutions pour distribuer des fichiers à 30 000 devices via ExpressRoute metered.

## Vue d'ensemble

Ce projet implémente et compare deux architectures :

- **Solution A** : Azure Blob Storage + API .NET 8 (User Delegation SAS)
- **Solution B** : MinIO HA sur AKS + API .NET 8 (Presigned URLs)

### Objectifs

- Mesurer les coûts totaux (storage, compute, ExpressRoute egress)
- Comparer les performances et la disponibilité
- Évaluer la complexité opérationnelle (OpEx)

## Structure du projet

```
.
├── shared/
│   └── models/                    # Modèles partagés (.NET 8)
├── solution-a-blob/
│   ├── src/API/                   # API Azure Blob + SAS
│   └── deployment/                # Terraform + Helm charts
├── solution-b-minio/
│   ├── src/API/                   # API MinIO + presigned URLs
│   └── deployment/                # Terraform + Helm charts
├── device-simulator/
│   └── src/                       # Simulateur de devices (.NET 8)
├── infrastructure/
│   └── terraform/                 # Infrastructure as Code
│       ├── aks/                   # Module AKS
│       ├── networking/            # VNet, Private Endpoints
│       └── storage/               # Azure Blob Storage
└── scripts/                       # Scripts PowerShell
    ├── build.ps1
    ├── deploy-infrastructure.ps1
    ├── deploy-solution-a.ps1
    ├── deploy-solution-b.ps1
    └── run-tests.ps1
```

## Prérequis

### Outils requis

- **.NET 8 SDK** : https://dotnet.microsoft.com/download/dotnet/8.0
- **Docker Desktop** : https://www.docker.com/products/docker-desktop
- **Azure CLI** : https://aka.ms/azure-cli
- **Terraform** : https://www.terraform.io/downloads
- **Helm** : https://helm.sh/docs/intro/install/
- **kubectl** : https://kubernetes.io/docs/tasks/tools/

### Compte Azure

- Compte Azure avec droits de création de ressources
- Souscription MSDN pour tests recommandée

## Guide de démarrage rapide

### 1. Cloner le repository

```powershell
git clone <repository-url>
cd FilesDownload
```

### 2. Build des projets

```powershell
.\scripts\build.ps1
```

### 3. Déployer l'infrastructure Azure

```powershell
# Créer un nom unique pour le Storage Account (3-24 caractères alphanumériques minuscules)
$storageAccountName = "stdevicemanifest$(Get-Random -Maximum 9999)"

.\scripts\deploy-infrastructure.ps1 -StorageAccountName $storageAccountName
```

Cette commande déploie :
- VNet avec subnets (AKS, Private Endpoints)
- AKS cluster (3-10 nodes auto-scaling)
- Azure Blob Storage avec Private Endpoint
- Log Analytics workspace

**Durée estimée** : 15-20 minutes

### 4. Créer un Azure Container Registry (ACR)

```powershell
az acr create --resource-group rg-devicemanifest-aks --name <your-acr-name> --sku Standard
```

### 5. Déployer Solution A (Azure Blob)

```powershell
.\scripts\deploy-solution-a.ps1 `
    -AcrName "<your-acr-name>" `
    -ClusterName "aks-devicemanifest" `
    -ResourceGroup "rg-devicemanifest-aks" `
    -StorageAccountName $storageAccountName
```

### 6. Déployer Solution B (MinIO)

```powershell
.\scripts\deploy-solution-b.ps1 `
    -AcrName "<your-acr-name>" `
    -ClusterName "aks-devicemanifest" `
    -ResourceGroup "rg-devicemanifest-aks"
```

### 7. Uploader des fichiers de test

#### Pour Solution A (Azure Blob)

```powershell
# Via Azure CLI
az storage blob upload-batch `
    --account-name $storageAccountName `
    --destination device-files `
    --source ./test-files `
    --auth-mode login
```

#### Pour Solution B (MinIO)

```powershell
# Port-forward MinIO console
kubectl port-forward svc/devicemanifest-solution-b-minio-console 9001:9001

# Accéder à http://localhost:9001
# Login: minioadmin / minioadmin123
# Créer bucket "device-files" et uploader des fichiers
```

### 8. Tester avec le Device Simulator

#### Test Solution A

```powershell
# Obtenir l'IP du service
$apiUrl = kubectl get svc devicemanifest-api-blob -o jsonpath='{.status.loadBalancer.ingress[0].ip}'

# Lancer le simulateur
.\scripts\run-tests.ps1 `
    -ApiUrl "http://$apiUrl" `
    -DeviceCount 100 `
    -Parallelism 10 `
    -DownloadFiles `
    -OutputFile "metrics-solution-a.json"
```

#### Test Solution B

```powershell
# Obtenir l'IP du service
$apiUrl = kubectl get svc devicemanifest-solution-b-api -o jsonpath='{.status.loadBalancer.ingress[0].ip}'

# Lancer le simulateur
.\scripts\run-tests.ps1 `
    -ApiUrl "http://$apiUrl" `
    -DeviceCount 100 `
    -Parallelism 10 `
    -DownloadFiles `
    -OutputFile "metrics-solution-b.json"
```

## Architecture détaillée

### Solution A : Azure Blob Storage

```
Device → API (.NET 8) → Azure Blob Storage
                ↓
         User Delegation SAS
                ↓
         Presigned URL (TTL 5-15 min)
```

**Avantages** :
- Service managé (pas d'exploitation)
- Haute disponibilité native (99.9%+)
- Coûts prévisibles
- Intégration Azure AD / Managed Identity
- Pas de gestion de stockage

**Inconvénients** :
- Coût storage Azure Blob
- Egress via ExpressRoute facturé
- Moins de contrôle granulaire

### Solution B : MinIO HA sur AKS

```
Device → API (.NET 8) → MinIO (4 replicas)
                ↓           ↓
         Presigned URL   Managed Disks (Premium SSD)
```

**Avantages** :
- Contrôle total sur le stockage
- API S3-compatible (portable)
- Potentiellement moins cher en storage (disques managés)

**Inconvénients** :
- OpEx : exploitation MinIO (monitoring, updates, backup)
- HA complexe à maintenir
- Coûts compute pour MinIO pods
- Risque de data loss si mal configuré

## Métriques collectées

Le Device Simulator collecte :

- **Manifest requests** : nombre, latence moyenne
- **File downloads** : nombre, bytes total, latence
- **Cache hits** : efficacité du caching (ETag)
- **Errors** : taux d'erreur
- **Throughput** : MB/s

Les métriques sont sauvegardées en JSON pour analyse.

## Analyse des coûts

### Coûts à considérer

1. **Compute** :
   - AKS nodes (Standard_D4s_v3)
   - MinIO pods (Solution B uniquement)

2. **Storage** :
   - Solution A : Azure Blob (Hot tier)
   - Solution B : Managed Premium SSD

3. **Network** :
   - ExpressRoute egress ($/GB metered)

4. **OpEx** :
   - Temps d'exploitation (monitoring, updates, incidents)

### Outils d'analyse

- Azure Cost Management
- Terraform cost estimation (Infracost)
- Métriques custom du simulator

## Maintenance

### Mise à jour des APIs

```powershell
# Rebuild et redéployer
.\scripts\build.ps1
.\scripts\deploy-solution-a.ps1 # ou solution-b
```

### Scaling AKS

```bash
az aks scale --resource-group rg-devicemanifest-aks --name aks-devicemanifest --node-count 5
```

### Monitoring

Utiliser Azure Monitor / Log Analytics pour surveiller :
- Latence des APIs
- Utilisation CPU/RAM des pods
- Erreurs HTTP
- Throughput réseau

## Nettoyage

```powershell
# Supprimer les déploiements Helm
helm uninstall devicemanifest-api-blob
helm uninstall devicemanifest-solution-b

# Détruire l'infrastructure Terraform
cd infrastructure/terraform
terraform destroy
```

## FAQ

### Comment augmenter le nombre de devices simulés ?

```powershell
.\scripts\run-tests.ps1 -ApiUrl "http://..." -DeviceCount 10000 -Parallelism 100
```

### Comment changer la TTL des presigned URLs ?

Modifier `ttl` dans l'appel API : `GET /api/manifest/{ceb}?ttl=15`

### Comment activer le HTTPS ?

Configurer un Ingress Controller (NGINX) avec certificat TLS dans les Helm charts.

## Support

Pour toute question, ouvrir une issue dans le repository.

## Licence

MIT
