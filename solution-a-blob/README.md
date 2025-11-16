# Solution A : Azure Blob Storage + API

Cette solution utilise Azure Blob Storage avec User Delegation SAS pour générer des URLs presignées sécurisées.

## Architecture

```
Device Request
    ↓
API (.NET 8 sur AKS)
    ↓
Azure Blob Storage (Private Endpoint)
    ↓
User Delegation Key (AAD-based)
    ↓
Presigned SAS URL (TTL 5-15 min)
    ↓
Device Download via SAS
```

## Composants

### API (.NET 8)

- **Endpoint** : `GET /api/manifest/{ceb}?ttl={minutes}`
- **Authentification** : Managed Identity → Storage Blob Data Contributor
- **Sécurité** : User Delegation SAS (pas de clés de compte)

### Fonctionnalités

1. **Génération de manifest** :
   - Liste tous les fichiers disponibles dans le container
   - Calcule ETag et SHA256 pour chaque fichier
   - Génère SAS URLs avec TTL configurable

2. **Conditional GET** :
   - Support de `If-None-Match` (ETag)
   - Retourne 304 si fichier inchangé

3. **Health check** : `GET /health`

## Déploiement

### Variables à configurer

Dans `deployment/helm/values.yaml` :

```yaml
config:
  azure:
    storageAccountName: "YOUR_STORAGE_ACCOUNT_NAME"
    containerName: "device-files"
```

### Deploiement avec script

```powershell
.\scripts\deploy-solution-a.ps1 `
    -AcrName "myacr" `
    -ClusterName "aks-devicemanifest" `
    -ResourceGroup "rg-devicemanifest-aks" `
    -StorageAccountName "stdevicemanifestpoc001"
```

### Déploiement manuel

```bash
# Build image
docker build -f src/API/Dockerfile -t myacr.azurecr.io/devicemanifest-api-blob:latest ../../

# Push to ACR
docker push myacr.azurecr.io/devicemanifest-api-blob:latest

# Deploy with Helm
cd deployment/helm
helm upgrade --install devicemanifest-api-blob . \
    --set image.repository=myacr.azurecr.io/devicemanifest-api-blob \
    --set config.azure.storageAccountName=stdevicemanifestpoc001
```

## Configuration

### Managed Identity

L'API utilise DefaultAzureCredential qui supporte :
- Managed Identity (production sur AKS)
- Azure CLI (développement local)
- Visual Studio / VS Code (développement local)

### Permissions requises

Le kubelet managed identity doit avoir le rôle **Storage Blob Data Contributor** sur le Storage Account.

Ceci est configuré automatiquement par Terraform dans `infrastructure/terraform/storage/main.tf`.

## Monitoring

### Logs

```bash
kubectl logs -l app.kubernetes.io/name=devicemanifest-api-blob --tail=100 -f
```

### Metrics

```bash
kubectl top pods -l app.kubernetes.io/name=devicemanifest-api-blob
```

## Tests

### Test local (sans Blob Storage)

Vous pouvez utiliser **Azurite** (émulateur local) :

```bash
docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0
```

### Test end-to-end

```powershell
# Port-forward vers le service
kubectl port-forward svc/devicemanifest-api-blob 5000:80

# Tester l'API
curl http://localhost:5000/api/manifest/CEB0000000000000001

# Lancer le simulateur
.\scripts\run-tests.ps1 -ApiUrl "http://localhost:5000" -DeviceCount 10
```

## Troubleshooting

### Erreur "403 Forbidden" lors de l'accès au Blob

- Vérifier que le Managed Identity a le rôle Storage Blob Data Contributor
- Vérifier que le Private Endpoint est correctement configuré
- Vérifier les règles réseau du Storage Account

### Erreur "SAS signature invalid"

- Vérifier que l'horloge du système est synchronisée (NTP)
- Augmenter le `StartsOn` offset (-5 minutes) dans le code

### Performance lente

- Vérifier la latence réseau vers le Storage Account
- Augmenter le nombre de replicas (HPA)
- Optimiser le calcul de SHA256 (stocker en metadata)

## Optimisations futures

1. **Caching** : Ajouter Redis pour cacher les manifests
2. **SHA256** : Pre-calculer et stocker dans metadata du blob
3. **CDN** : Ajouter Azure CDN (si public)
4. **Compression** : Activer compression gzip sur les blobs
