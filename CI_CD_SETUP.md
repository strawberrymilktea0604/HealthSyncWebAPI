# HealthSync CI/CD & Environment Setup

This document describes the complete CI/CD pipeline and environment configuration for HealthSync API.

## Summary of Changes

### 1. ✅ Environment Variables Separation (Dev/Prod)

Created two environment files:

| File | Purpose | Usage |
|------|---------|-------|
| **.env.dev** | Development secrets & config | `docker-compose up --env-file .env.dev` |
| **.env.prod** | Production secrets & config | `docker-compose -f docker-compose.prod.yml --env-file .env.prod up` |

**Key differences**:

```
Development (.env.dev):
- DB_SA_PASSWORD=YourStrong!Passw0rd123 (hardcoded for dev)
- ASPNETCORE_ENVIRONMENT=Development
- JWT_SECRET_KEY=dev-secret-key-... (weak, for testing)
- MinIO local credentials (minioadmin/minioadmin)

Production (.env.prod):
- DB_SA_PASSWORD=${PROD_DB_SA_PASSWORD} (from CI/CD secrets)
- ASPNETCORE_ENVIRONMENT=Production
- JWT_SECRET_KEY=${PROD_JWT_SECRET_KEY} (strong, from vault)
- MinIO production credentials (from secrets manager)
```

### 2. ✅ Production Docker Compose File

Created **docker-compose.prod.yml** with:

- **Production-grade configuration**:
  - MSSQL Standard Edition (instead of Express)
  - Health checks for all services (liveness + readiness)
  - `restart: unless-stopped` policy
  - Proper dependencies with `service_healthy` condition
  - Production-optimized resource limits

- **Key differences from dev**:
  ```yaml
  # Dev: simple hardcoded values
  # Prod: environment variable placeholders
  - SA_PASSWORD=${DB_SA_PASSWORD}
  - MinIO credentials from env vars
  - Image: healthsync-api:latest (pre-built, not local build)
  - Health checks with retries
  - Longer startup timeouts
  ```

### 3. ✅ Updated docker-compose.yml for Development

Modified **docker-compose.yml** to:
- Use `.env.dev` file for environment variables
- Support parameterized configuration
- Add Jenkins service for CI/CD
- Remain simple and dev-friendly

### 4. ✅ Jenkins Service Added

Added to **docker-compose.yml**:

```yaml
jenkins:
  image: jenkins/jenkins:lts-alpine
  container_name: healthsync-jenkins
  ports:
    - "8081:8080"      # Web UI
    - "50000:50000"     # Agent communication
  volumes:
    - jenkins_data:/var/jenkins_home
    - /var/run/docker.sock:/var/run/docker.sock
```

**Why LTS Alpine**:
- Lightweight (~600MB vs ~1.5GB for full Jenkins)
- Long-term support (stable releases)
- Alpine base reduces security attack surface
- Perfect for Docker environments

### 5. ✅ Jenkinsfile with Build Pipeline

Created **Jenkinsfile** with 10 stages:

| Stage | Purpose | Details |
|-------|---------|---------|
| **Checkout** | Clone repository | Uses GitHub credentials |
| **Prepare Environment** | Load .env file | Selects dev or prod |
| **Build Solution** | Compile .NET | `dotnet build` Release mode |
| **Run Unit Tests** | Test execution | Finds .Tests.csproj projects |
| **Build Docker Image** | Create Docker image | Tags with build number |
| **Push Docker Image** | Registry push | Dev: skip, Prod: push |
| **Deploy Stack** | Run docker-compose | Dev or Prod compose file |
| **Health Check** | Verify deployment | Polls API /health endpoint |
| **Post: Always** | Cleanup | Archives logs |
| **Post: Success/Failure** | Notifications | Extensible (Slack, email, etc.) |

## Quick Start Guide

### Prerequisites

```powershell
# Ensure Docker and Docker Compose installed
docker --version
docker-compose --version

# Ensure .NET 8 SDK installed
dotnet --version
```

### Development Environment

**Step 1: Start services**
```powershell
# Uses .env.dev automatically
docker-compose up -d

# Verify all services
docker-compose ps
```

**Step 2: Access services**
- API: http://localhost:8080/swagger
- NGINX (HTTPS): https://localhost/swagger (with -k flag)
- MinIO UI: http://localhost:9001 (minioadmin/minioadmin)
- SQL Server: localhost:1433 (sa/YourStrong!Passw0rd123)
- Jenkins: http://localhost:8081

**Step 3: Test the pipeline**
```powershell
# Start Jenkins for testing
docker-compose up -d jenkins

# Wait for initialization (2-3 minutes)
docker-compose logs -f jenkins

# Access Jenkins UI
# URL: http://localhost:8081
# Password: docker-compose exec jenkins cat /var/jenkins_home/secrets/initialAdminPassword
```

### Production Deployment

**Step 1: Set production secrets**

Create a secure `.env.prod` file with actual values:

```bash
# .env.prod
DB_SA_PASSWORD=SuperSecure!DBPass123
MINIO_ACCESS_KEY=prod-access-key
MINIO_SECRET_KEY=prod-secret-key
JWT_SECRET_KEY=production-jwt-secret-min-32-chars-long
GOOGLE_CLIENT_ID=prod-google-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=prod-google-secret
```

**Step 2: Deploy**
```powershell
# Build and push Docker image (via Jenkins or manually)
docker build -t healthsync-api:latest -f Dockerfile .

# Deploy with production compose file
docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d

# Verify health
docker-compose -f docker-compose.prod.yml ps
```

**Step 3: Verify deployment**
```powershell
# Check all services are healthy
docker-compose -f docker-compose.prod.yml ps

# View logs
docker-compose -f docker-compose.prod.yml logs -f api

# Test API
curl https://localhost/api/v1/health -k
```

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                    Development (Local)                     │
│                                                             │
│  docker-compose.yml (uses .env.dev)                       │
│  ├── DB (SQL Server Express)                              │
│  ├── MinIO (Local S3)                                     │
│  ├── API (.NET 8 app)                                     │
│  ├── NGINX (Reverse proxy)                                │
│  └── Jenkins (CI/CD server)                               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                          │
                          │ Git push to main
                          │
                          v
         ┌─────────────────────────────────┐
         │       GitHub Repository         │
         │    (strawberrymilktea0604/      │
         │     HealthSyncWebAPI)           │
         └──────────────┬────────────────┘
                        │ Webhook trigger
                        │
                        v
         ┌─────────────────────────────────┐
         │      Jenkins Pipeline           │
         │  (Jenkinsfile stages)           │
         │  1. Checkout                    │
         │  2. Build .NET                  │
         │  3. Run tests                   │
         │  4. Build Docker image          │
         │  5. Push to registry            │
         │  6. Deploy (dev or prod)        │
         └──────────────┬────────────────┘
                        │
            ┌───────────┴───────────┐
            │                       │
            v                       v
  ┌────────────────┐      ┌──────────────────┐
  │  Dev Deploy    │      │  Prod Deploy     │
  │  (.env.dev)    │      │  (.env.prod)     │
  │  Quick test    │      │  Full stack      │
  │  Local access  │      │  HA capable      │
  └────────────────┘      └──────────────────┘
            │                       │
            v                       v
   Dev docker-compose    docker-compose.prod.yml
   Stack                 Stack
```

## File Structure

```
HealthSyncWebAPI/
├── .env.dev                      # Dev environment (included in git, safe)
├── .env.prod                     # Prod environment (SHOULD NOT be in git)
├── docker-compose.yml            # Dev/local compose (uses .env.dev)
├── docker-compose.prod.yml       # Prod compose (uses .env.prod)
├── Dockerfile                    # Multi-stage build for API
├── nginx.conf                    # NGINX config
├── Jenkinsfile                   # CI/CD pipeline stages
├── generate-certs.sh             # SSL cert generation (Linux)
├── generate-certs.ps1            # SSL cert generation (Windows)
├── NGINX_SETUP.md               # NGINX documentation
├── JENKINS_SETUP.md             # Jenkins setup guide
├── HealthSyncWebAPI.sln         # .NET solution
├── HealthSync.Domain/           # Core entities
├── HealthSync.Application/      # Business logic
├── HealthSync.Infrastructure/   # Data access
└── HealthSync.WebApi/           # API endpoints
```

## Environment Variables Explained

### Common to Both Dev & Prod

```properties
# Database
DB_DATABASE=HealthSyncDb

# MinIO
MINIO_BUCKET_NAME=healthsync-images

# JWT Settings
JWT_ISSUER=HealthSyncAPI
JWT_AUDIENCE=HealthSyncClient
JWT_ACCESS_TOKEN_EXPIRATION_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRATION_DAYS=7

# NGINX
NGINX_PORT_HTTP=80
NGINX_PORT_HTTPS=443
```

### Dev-Specific (.env.dev)

```properties
DB_SA_PASSWORD=YourStrong!Passw0rd123         # Hardcoded for local dev
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=minioadmin
JWT_SECRET_KEY=dev-secret-key-...            # Weak for testing
ASPNETCORE_ENVIRONMENT=Development
```

### Prod-Specific (.env.prod)

```properties
DB_SA_PASSWORD=${PROD_DB_SA_PASSWORD}         # From CI/CD secrets
MINIO_ACCESS_KEY=${PROD_MINIO_ACCESS_KEY}
MINIO_SECRET_KEY=${PROD_MINIO_SECRET_KEY}
JWT_SECRET_KEY=${PROD_JWT_SECRET_KEY}         # Strong from vault
ASPNETCORE_ENVIRONMENT=Production
```

## Jenkins Pipeline Flow

```
┌─ GitHub Webhook ──────────────┐
│   (or manual trigger)         │
└───────────────┬───────────────┘
                │
                v
        ┌─ Checkout ─────────────┐
        │ Git clone              │
        │ Branch: main (default) │
        └────────────┬───────────┘
                     │
                     v
        ┌─ Prepare Environment ──┐
        │ Copy .env file         │
        │ Load variables         │
        └────────────┬───────────┘
                     │
                     v
        ┌─ Build Solution ───────┐
        │ dotnet build Release   │
        │ Compile .NET projects  │
        └────────────┬───────────┘
                     │
                     v
        ┌─ Run Unit Tests ───────┐
        │ dotnet test            │
        │ Report results         │
        └────────────┬───────────┘
                     │
                     v
        ┌─ Build Docker Image ───┐
        │ docker build           │
        │ Tag with build number  │
        └────────────┬───────────┘
                     │
                     v
        ┌─ Push to Registry ─────┐
        │ (Prod only)            │
        │ docker push            │
        └────────────┬───────────┘
                     │
                     v
        ┌─ Deploy Stack ─────────┐
        │ docker-compose up      │
        │ (dev or prod)          │
        └────────────┬───────────┘
                     │
                     v
        ┌─ Health Check ─────────┐
        │ Poll /health endpoint  │
        │ Verify deployment      │
        └────────────┬───────────┘
                     │
        ┌────────────┴────────────┐
        │                         │
        v (Success)              v (Failure)
    ┌─────────┐              ┌─────────┐
    │ SUCCESS │              │ FAILURE │
    └─────────┘              └─────────┘
```

## Security Best Practices

### Development
- ✅ Use weak secrets (hardcoded) for local testing
- ✅ All services expose ports (localhost only)
- ✅ Self-signed HTTPS certificates
- ✅ MinIO default credentials (minioadmin/minioadmin)

### Production
- ✅ **NEVER** commit `.env.prod` to Git
- ✅ Store secrets in: Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault
- ✅ Use strong, randomly generated secrets (min 32 chars for JWT)
- ✅ Rotate secrets regularly (quarterly minimum)
- ✅ Use trusted SSL certificates (Let's Encrypt, DigiCert)
- ✅ Restrict service ports (only NGINX on 80/443)
- ✅ Enable MSSQL authentication (SA account in prod should have complex password)
- ✅ Configure database backups and replication
- ✅ Enable audit logging and monitoring

### Jenkins Security
- ✅ Change default admin password immediately
- ✅ Use GitHub Personal Access Token (not password)
- ✅ Store Jenkins credentials encrypted
- ✅ Restrict job access (role-based)
- ✅ Disable script approval for untrusted builds
- ✅ Monitor Jenkins access logs

## Troubleshooting

### Dev Stack Won't Start
```powershell
# Check .env.dev exists and is valid
Test-Path .env.dev

# View startup logs
docker-compose logs db api

# Check disk space
df -h

# Restart services
docker-compose down
docker-compose up -d
```

### Jenkins Initialization Hangs
```powershell
# Give Jenkins more time (5 minutes)
# Or check logs for Java errors
docker-compose logs jenkins | tail -100

# Restart Jenkins
docker-compose restart jenkins
```

### Pipeline Build Fails
1. Check Jenkins logs: `docker-compose logs -f jenkins`
2. Check build output in Jenkins UI: http://localhost:8081
3. View API logs: `docker-compose logs api`
4. Verify Dockerfile exists and is valid
5. Check GitHub credentials are correct

### Health Check Times Out
```powershell
# Verify API is running
curl http://localhost:8080/health

# Check API logs
docker-compose logs api

# Increase health check timeout in Jenkinsfile if needed
```

## Next Steps

1. **Setup Monitoring** (Prometheus + Grafana)
   - Monitor API metrics, memory, CPU, response times
   - Track deployments and build times

2. **Setup Logging** (ELK Stack)
   - Centralized log aggregation
   - Structured logs from API

3. **Setup Alerts** (Alertmanager)
   - Email/Slack notifications on deployment failures
   - API health alerts

4. **Database Replication** (Prod only)
   - SQL Server Availability Groups
   - Backup and disaster recovery

5. **Load Testing**
   - Apache JMeter or Locust
   - Verify performance under load

## References

- [Docker Compose Official Docs](https://docs.docker.com/compose/)
- [Jenkins Official Documentation](https://www.jenkins.io/doc/)
- [Jenkinsfile Syntax](https://www.jenkins.io/doc/book/pipeline/syntax/)
- [GitHub Personal Access Token](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token)
- [MSSQL in Docker](https://hub.docker.com/_/microsoft-mssql-server)
- [MinIO Documentation](https://docs.min.io/)
- [NGINX Documentation](https://nginx.org/en/docs/)
