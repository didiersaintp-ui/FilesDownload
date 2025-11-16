# Solution B : MinIO HA + API

Cette solution utilise MinIO (S3-compatible) déployé en mode distribué sur AKS avec une API .NET 8.

## Architecture

```
Device Request
    ↓
API (.NET 8 sur AKS)
    ↓
MinIO (4 replicas, distributed mode)
    ↓
Presigned GET URL (TTL 5-15 min)
    ↓
Device Download via presigned URL
```

## Composants

### MinIO

- **Mode** : Distributed (4 replicas)
- **Storage** : Managed Premium SSD (500 GB par replica)
- **Haute disponibilité** : Erasure coding, self-healing
- **Console** : Interface web pour administration

### API (.NET 8)

- **Endpoint** : `GET /api/manifest/{ceb}?ttl={minutes}`
- **Client** : Minio .NET SDK
- **Sécurité** : Presigned URLs avec credentials stockés en Secret

## Déploiement

### Structure Helm

Le chart déploie :
1. **MinIO** (via chart officiel comme dépendance)
2. **API** (custom deployment)

### Variables à configurer

Dans `deployment/helm/values.yaml` :

```yaml
minio:
  rootUser: minioadmin
  rootPassword: minioadmin123  # CHANGER EN PRODUCTION!

api:
  config:
    minio:
      endpoint: "devicemanifest-solution-b-minio:9000"
      accessKey: "minioadmin"
      secretKey: "minioadmin123"
```

### Deploiement avec script

```powershell
.\scripts\deploy-solution-b.ps1 `
    -AcrName "myacr" `
    -ClusterName "aks-devicemanifest" `
    -ResourceGroup "rg-devicemanifest-aks"
```

### Déploiement manuel

```bash
# Build image
docker build -f src/API/Dockerfile -t myacr.azurecr.io/devicemanifest-api-minio:latest ../../

# Push to ACR
docker push myacr.azurecr.io/devicemanifest-api-minio:latest

# Add MinIO Helm repo
helm repo add minio https://charts.min.io/
helm repo update

# Deploy with Helm
cd deployment/helm
helm dependency build
helm upgrade --install devicemanifest-solution-b . \
    --set api.image.repository=myacr.azurecr.io/devicemanifest-api-minio \
    --wait --timeout 10m
```

## Administration MinIO

### Accéder à la console

```bash
# Port-forward vers la console
kubectl port-forward svc/devicemanifest-solution-b-minio-console 9001:9001

# Ouvrir http://localhost:9001
# Login: minioadmin / minioadmin123
```

### Créer un bucket

Via la console ou CLI :

```bash
# Port-forward API MinIO
kubectl port-forward svc/devicemanifest-solution-b-minio 9000:9000

# Utiliser mc (MinIO Client)
mc alias set myminio http://localhost:9000 minioadmin minioadmin123
mc mb myminio/device-files
```

### Uploader des fichiers

```bash
mc cp ./test-files/* myminio/device-files/
```

## Monitoring

### Logs MinIO

```bash
kubectl logs -l app=minio --tail=100 -f
```

### Logs API

```bash
kubectl logs -l app.kubernetes.io/component=api --tail=100 -f
```

### Metrics

```bash
kubectl top pods -l app.kubernetes.io/instance=devicemanifest-solution-b
```

### MinIO Metrics

MinIO expose des métriques Prometheus sur `/minio/v2/metrics/cluster`.

## Haute disponibilité

### Erasure Coding

Avec 4 replicas, MinIO utilise EC:2 (2 data shards, 2 parity shards).

- Peut perdre jusqu'à 2 disques sans perte de données
- Overhead de 2x en stockage

### Self-healing

MinIO détecte et répare automatiquement les corruptions de données.

### Backup

Important : MinIO n'est PAS un backup !

Pour backup :

```bash
# Utiliser mc mirror
mc mirror myminio/device-files /backup/device-files
```

Ou utiliser Azure Backup pour les Persistent Volumes.

## Performance

### Tuning

1. **Augmenter les replicas** (4 → 8) pour plus de throughput
2. **Utiliser Premium SSD** pour IOPS élevés
3. **Ajuster resources.requests/limits** selon la charge

### Benchmarks

```bash
# MinIO benchmark tool
mc admin speedtest myminio
```

## Sécurité

### Credentials

En production, utiliser :
- **Azure Key Vault** pour stocker credentials
- **Kubernetes Secrets** (sealed-secrets)
- **Rotation** régulière des credentials

### Network Policies

Restreindre l'accès au MinIO :

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: minio-network-policy
spec:
  podSelector:
    matchLabels:
      app: minio
  ingress:
  - from:
    - podSelector:
        matchLabels:
          app.kubernetes.io/component: api
```

## Troubleshooting

### MinIO pods ne démarrent pas

- Vérifier les Persistent Volume Claims (PVC)
- Vérifier les quotas de disques
- Logs : `kubectl describe pod <minio-pod>`

### Erreur "connection refused"

- Vérifier que le service MinIO est créé : `kubectl get svc`
- Vérifier le DNS : `nslookup devicemanifest-solution-b-minio`

### Performance lente

- Vérifier IOPS des disques (Premium SSD recommandé)
- Augmenter les resources CPU/RAM de MinIO
- Vérifier network latency

### Data loss

- Vérifier status des drives : `mc admin info myminio`
- Vérifier si healing est en cours : `mc admin heal myminio`

## Coûts

### Calcul estimé (30 jours)

Avec 4 replicas × 500 GB Premium SSD :

- **Storage** : 4 × 500 GB × $0.12/GB = ~$240/mois
- **Compute** : Inclus dans les nodes AKS (partagé)

Comparer avec Solution A pour le même volume.

## Optimisations futures

1. **Tiering** : Utiliser S3 Lifecycle pour archiver les vieux fichiers
2. **Compression** : Activer compression côté MinIO
3. **Caching** : Ajouter Redis pour cacher les manifests
4. **Geo-replication** : Site replication pour DR
