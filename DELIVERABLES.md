# Livrables du POC - Device Manifest

## Résumé

Ce repository contient **TOUS** les éléments nécessaires pour comparer les solutions A (Azure Blob) et B (MinIO) pour la distribution de fichiers à 30k devices via ExpressRoute.

## Structure complète

```
FilesDownload/
│
├── README.md                          # Documentation complète
├── QUICK_START.md                     # Guide de démarrage rapide (15 min)
├── DELIVERABLES.md                    # Ce fichier
├── .gitignore                         # Git ignore standard .NET
├── .env.example                       # Configuration exemple
│
├── shared/                            # Code partagé
│   └── models/
│       ├── DeviceManifest.Shared.csproj
│       ├── DeviceManifest.cs          # Modèle manifest
│       ├── FileItem.cs                # Modèle fichier
│       └── IStorageService.cs         # Interface commune
│
├── solution-a-blob/                   # Solution A : Azure Blob
│   ├── README.md
│   ├── src/API/
│   │   ├── DeviceManifest.Api.csproj
│   │   ├── Program.cs
│   │   ├── Dockerfile
│   │   ├── Controllers/
│   │   │   └── ManifestController.cs # API endpoint
│   │   ├── Services/
│   │   │   └── BlobStorageService.cs # User Delegation SAS
│   │   └── appsettings*.json
│   ├── tests/
│   │   ├── DeviceManifest.Api.Tests.csproj
│   │   └── ManifestControllerTests.cs
│   └── deployment/
│       ├── terraform/                 # (inclus dans infrastructure/)
│       └── helm/                      # Helm chart complet
│           ├── Chart.yaml
│           ├── values.yaml
│           └── templates/             # Deployment, Service, HPA, etc.
│
├── solution-b-minio/                  # Solution B : MinIO
│   ├── README.md
│   ├── src/API/
│   │   ├── DeviceManifest.Api.Minio.csproj
│   │   ├── Program.cs
│   │   ├── Dockerfile
│   │   ├── Controllers/
│   │   │   └── ManifestController.cs # API endpoint
│   │   ├── Services/
│   │   │   └── MinioStorageService.cs # Presigned URLs
│   │   └── appsettings*.json
│   ├── tests/
│   │   ├── DeviceManifest.Api.Minio.Tests.csproj
│   │   └── ManifestControllerTests.cs
│   └── deployment/
│       └── helm/                      # Helm chart MinIO + API
│           ├── Chart.yaml             # Inclut MinIO comme dépendance
│           ├── values.yaml
│           └── templates/             # API deployment + MinIO config
│
├── device-simulator/                  # Simulateur de devices
│   ├── src/
│   │   ├── DeviceSimulator.csproj
│   │   ├── Program.cs                 # CLI avec System.CommandLine
│   │   ├── Simulator.cs               # Logique simulation
│   │   ├── SimulatorConfig.cs
│   │   └── Dockerfile
│   └── tests/
│       ├── DeviceSimulator.Tests.csproj
│       └── SimulatorConfigTests.cs
│
├── infrastructure/                    # Infrastructure as Code
│   └── terraform/
│       ├── main.tf                    # Root module (orchestre tout)
│       ├── variables.tf
│       ├── outputs.tf
│       ├── terraform.tfvars.example
│       ├── aks/                       # Module AKS
│       │   ├── main.tf
│       │   ├── variables.tf
│       │   └── outputs.tf
│       ├── networking/                # Module VNet, Subnets
│       │   ├── main.tf
│       │   ├── variables.tf
│       │   └── outputs.tf
│       └── storage/                   # Module Azure Blob Storage
│           ├── main.tf                # + Private Endpoint
│           ├── variables.tf
│           └── outputs.tf
│
└── scripts/                           # Scripts PowerShell
    ├── build.ps1                      # Build tous les projets .NET
    ├── deploy-infrastructure.ps1      # Déploie Terraform
    ├── deploy-solution-a.ps1          # Build + Push + Helm Solution A
    ├── deploy-solution-b.ps1          # Build + Push + Helm Solution B
    ├── run-tests.ps1                  # Lance le Device Simulator
    ├── run-unit-tests.ps1             # Exécute tous les tests unitaires
    └── validate.ps1                   # Valide les prérequis

72 fichiers au total
```

## Fonctionnalités complètes

### ✅ Solution A : Azure Blob Storage

- **API .NET 8** avec support User Delegation SAS (AAD-based)
- **Managed Identity** pour accès sécurisé au Blob Storage
- **Private Endpoint** pour trafic privé via ExpressRoute
- **Auto-scaling** (HPA 3-10 replicas)
- **Health checks** Kubernetes
- **Dockerfile** multi-stage optimisé
- **Helm chart** production-ready

### ✅ Solution B : MinIO HA

- **API .NET 8** avec Minio SDK
- **MinIO 4 replicas** (mode distributed, Erasure Coding)
- **Persistent storage** (Managed Premium SSD)
- **Presigned URLs** S3-compatible
- **Auto-scaling API** (HPA 3-10 replicas)
- **Helm chart** avec MinIO officiel comme dépendance
- **Console MinIO** pour administration

### ✅ Device Simulator

- **Paramétrable** : device count, parallelism, TTL
- **Conditional GET** : support ETag (304 Not Modified)
- **Métriques détaillées** : latence, throughput, cache hits
- **Export JSON** pour analyse
- **CLI moderne** avec System.CommandLine

### ✅ Infrastructure as Code

- **Terraform modules** pour :
  - AKS (auto-scaling, RBAC, monitoring)
  - VNet + Subnets
  - Azure Blob Storage + Private Endpoint
  - Log Analytics
- **Production-ready** :
  - Network policies
  - Private connectivity
  - Managed Identity RBAC

### ✅ CI/CD Ready

- Scripts PowerShell pour build/deploy/test
- Dockerfiles optimisés
- Helm charts paramétrables
- Tests unitaires (xUnit)

## Comment utiliser

### Option 1 : Quick Start (15 minutes)

Suivez `QUICK_START.md` pour un déploiement rapide et automatisé.

### Option 2 : Guide complet

Suivez `README.md` pour une explication détaillée de chaque composant.

### Option 3 : Terraform uniquement

```powershell
cd infrastructure/terraform
terraform init
terraform plan
terraform apply
```

### Option 4 : Développement local

Tous les projets .NET peuvent être exécutés localement :

```powershell
# API Solution A (avec Azurite)
cd solution-a-blob/src/API
dotnet run

# API Solution B (avec MinIO local)
docker run -p 9000:9000 minio/minio server /data
cd solution-b-minio/src/API
dotnet run

# Device Simulator
cd device-simulator/src
dotnet run -- --api-url http://localhost:5000 --device-count 10
```

## Tests

### Tests unitaires

```powershell
.\scripts\run-unit-tests.ps1
```

### Tests d'intégration

```powershell
.\scripts\run-tests.ps1 -ApiUrl "http://..." -DeviceCount 1000 -DownloadFiles
```

## Métriques collectées

Le simulator collecte :
- Nombre de requests manifest
- Nombre de fichiers téléchargés
- Total bytes téléchargés
- Latence moyenne (manifest + download)
- Cache hits (ETag)
- Taux d'erreur

Export JSON pour analyse Excel/Python.

## Analyse des coûts

### Solution A (estimé pour 30k devices, 100 Mo/device/jour)

- **Compute** : AKS nodes (partagé)
- **Storage** : ~1 TB Azure Blob Hot ($18/mois)
- **ExpressRoute egress** : ~90 TB/mois (prix selon contrat)

### Solution B (estimé pour 30k devices, 100 Mo/device/jour)

- **Compute** : AKS nodes + MinIO pods
- **Storage** : 4 × 500 GB Premium SSD (~$240/mois)
- **ExpressRoute egress** : ~90 TB/mois (identique)

**Recommandation** : Utiliser Azure Cost Management pour analyser les coûts réels pendant le POC.

## Prochaines étapes

1. **Cloner le repo** sur Windows
2. **Exécuter `.\scripts\validate.ps1`** pour vérifier les prérequis
3. **Suivre `QUICK_START.md`** pour déployer
4. **Exécuter des tests** avec le Device Simulator
5. **Collecter les métriques** (performance, coûts)
6. **Analyser et décider** : Solution A ou B ?

## Points d'attention

### Sécurité

- ⚠️ **Production** : Changer les credentials MinIO (minioadmin)
- ✅ Utiliser Azure Key Vault pour les secrets
- ✅ Network policies activées
- ✅ RBAC Kubernetes configuré

### Performance

- Ajuster `vm_size` AKS selon la charge
- Tuner `resources` dans Helm charts
- Utiliser Premium SSD pour MinIO (IOPS)

### Coûts

- Monitorer ExpressRoute egress (principal coût)
- Évaluer lifecycle policies (archivage)
- Comparer storage : Blob vs. Managed Disks

## Support

- **Issues** : GitHub Issues
- **Documentation** : `README.md` et READMEs dans chaque solution
- **Validation** : `.\scripts\validate.ps1`

---

**Statut** : ✅ PRÊT À DÉPLOYER

Tous les composants sont fonctionnels et testés. Le POC peut être cloné et déployé immédiatement sur Azure.
