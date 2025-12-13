# Git Commit Guidelines - HealthSync CI/CD Files

This guide explains which files should and should NOT be committed to Git.

## Quick Reference

| File/Folder | Commit? | Reason |
|-------------|---------|--------|
| ✅ **Jenkinsfile** | YES | Pipeline configuration (shared across team) |
| ✅ **docker-compose.prod.yml** | YES | Production stack config (shared across team) |
| ✅ **.env.dev** | YES | Development environment (safe, no secrets) |
| ❌ **.env.prod** | NO | Production secrets (DB password, JWT key) |
| ❌ **certs/** | NO | SSL certificates (regenerate per environment) |
| ❌ **jenkins_data/** | NO | Jenkins local volume (container-specific) |
| ✅ **nginx.conf** | YES | Configuration file (shared) |
| ✅ **generate-certs.sh** | YES | Script to generate certs (not certs themselves) |
| ✅ **JENKINS_SETUP.md** | YES | Documentation |
| ✅ **CI_CD_SETUP.md** | YES | Documentation |

---

## Detailed Explanation

### ✅ Files TO COMMIT

#### 1. **Jenkinsfile**
```groovy
// Contains: Pipeline stages, build logic, deployment steps
// Why commit: Entire team needs same pipeline definition
// Why safe: No secrets hardcoded (uses credentials objects)
```

**Action**: Commit to main branch
```powershell
git add Jenkinsfile
git commit -m "Add Jenkins CI/CD pipeline"
git push origin main
```

---

#### 2. **docker-compose.prod.yml**
```yaml
# Contains: Service definitions, health checks, restart policies
# Why commit: Template for production stack
# Why safe: Secrets come from environment variables (${PROD_JWT_SECRET_KEY})
```

**Action**: Commit to main branch
```powershell
git add docker-compose.prod.yml
git commit -m "Add production docker-compose configuration"
git push origin main
```

---

#### 3. **.env.dev** (Development environment)
```properties
# Contains: Dev passwords (hardcoded for testing)
# Example: DB_SA_PASSWORD=CHANGE_ME
# Why commit: Safe for development, no production data
# Usage: Local testing only, different from production
```

**Action**: Commit to main branch
```powershell
git add .env.dev
git commit -m "Add development environment variables"
git push origin main
```

**Note**: Since `.env.dev` is committed, developers will have it for local testing.

---

#### 4. **nginx.conf**
```nginx
# Contains: NGINX configuration, proxy rules, security headers
# Why commit: Shared infrastructure config
# Why safe: No credentials or sensitive data
```

**Action**: Already should be committed
```powershell
git status  # Should show "nothing to commit" for nginx.conf
```

---

#### 5. **Documentation Files**
- `JENKINS_SETUP.md` - Setup guide
- `CI_CD_SETUP.md` - Complete CI/CD guide
- `NGINX_SETUP.md` - NGINX guide

**Action**: Already committed (create if needed)

---

### ❌ Files NOT TO COMMIT

#### 1. **.env.prod** (Production environment)
```properties
# Contains: DB_SA_PASSWORD=ActualProdPassword123
#          JWT_SECRET_KEY=production-secret-min-32-chars
# Why ignore: SENSITIVE PRODUCTION SECRETS
# Risk: If leaked, attacker can access production database
```

**Action**: Add to `.gitignore` (already done)
```gitignore
.env.prod
.env.production
.env.*.local
```

**Correct workflow**:
1. `.env.prod` stored securely (Azure Key Vault, AWS Secrets Manager)
2. CI/CD pipeline (Jenkins) retrieves secrets at runtime
3. `.env.prod` file **never** committed to Git

---

#### 2. **certs/** (SSL Certificates)
```
certs/
├── nginx.crt     ❌ Don't commit
└── nginx.key     ❌ Don't commit (PRIVATE KEY!)
```

**Why ignore**:
- Private key is **sensitive**
- Certificates are environment-specific
- Should be regenerated per environment

**Action**: Already in `.gitignore`
```gitignore
certs/
*.crt
*.key
*.pem
```

**Correct workflow**:
1. Generate locally: `.\generate-certs.ps1`
2. For production: Use Let's Encrypt + certbot
3. Certs not in Git, but Dockerfiles mount them at runtime

---

#### 3. **jenkins_data/** (Jenkins volume)
```
jenkins_data/
├── jobs/           ❌ Local Jenkins jobs
├── secrets/        ❌ Jenkins credentials
└── updates/        ❌ Plugin cache
```

**Why ignore**:
- Container-specific data
- Each Jenkins instance is different
- Credentials stored locally (not in code)

**Action**: Add to `.gitignore`
```gitignore
jenkins_data/
```

**Correct workflow**:
1. Jenkins runs in container
2. Volume persists locally: `docker-compose up -d`
3. Jobs configured via Jenkins UI (not committed)

---

#### 4. **appsettings.Development.json** & **appsettings.Production.json**
```json
// Already ignored in .gitignore
// Contains: Logging levels, API keys, connection strings
```

**Action**: Already in `.gitignore` (no changes needed)

---

#### 5. **docker-compose.override.yml**
```yaml
# Local overrides for docker-compose.yml
# Example: Different ports, local databases, debug settings
```

**Why ignore**: Personal local testing file

**Action**: Already in `.gitignore` (no changes needed)

---

## Complete .gitignore Changes

### Updated sections:

```gitignore
## Secrets & environment
appsettings.*.json
!appsettings.json
appsettings.Development.json
appsettings.Production.json
secrets.json

# Production secrets MUST be ignored
.env.prod
.env.production
.env.*.local

# Development .env.dev CAN be committed (no sensitive data)
# Uncomment below if you want to allow it:
# !.env.dev

## Docker & Deployment
docker-volumes/
docker-compose.override.yml
jenkins_data/              ← NEWLY ADDED
.docker/                   ← NEWLY ADDED

## SSL Certificates (already ignored)
certs/
*.crt
*.key
*.pem
```

---

## Production Deployment Checklist

Before deploying to production:

```bash
# 1. Verify .env.prod is NOT in Git
git ls-files | grep -i ".env.prod"
# Output should be empty

# 2. Verify secrets are not in recent commits
git log -p --all -S "PROD_DB_SA_PASSWORD" | head -20
# Should return nothing

# 3. Create .env.prod securely (NOT in Git)
# Option A: Azure Key Vault
# Option B: AWS Secrets Manager
# Option C: HashiCorp Vault
# Option D: Manual setup at deployment time

# 4. Set up CI/CD secrets in Jenkins
# Jenkins UI → Manage Jenkins → Configure System
# Add: PROD_DB_SA_PASSWORD, PROD_JWT_SECRET_KEY, etc.

# 5. Deploy using Jenkins pipeline
# Push to main → Jenkins webhook triggers
# Pipeline retrieves secrets from Jenkins credentials
# docker-compose.prod.yml uses environment variables
```

---

## FAQ

### Q: Can I commit `.env.dev`?
**A**: Yes! `.env.dev` contains only development secrets safe for local testing. It's different from `.env.prod`.

**Why it's safe**:
- Credentials are generic (minioadmin, CHANGE_ME)
- No production data
- Every developer gets same config
- Not used in production

### Q: What if I accidentally commit `.env.prod`?
**A**: 

1. **Immediately revoke the secret**:
   ```bash
   # Azure Key Vault
   az keyvault secret delete --vault-name MyVault --name PROD_DB_SA_PASSWORD
   
   # Generate new secret
   az keyvault secret set --vault-name MyVault --name PROD_DB_SA_PASSWORD --value NewSecretValue123
   ```

2. **Remove from Git history**:
   ```bash
   # Using BFG Repo-Cleaner (easier)
   bfg --delete-files .env.prod
   
   # Or git filter-branch (harder)
   git filter-branch --tree-filter 'rm -f .env.prod' HEAD
   ```

3. **Force push (team coordination needed)**:
   ```bash
   git push origin main --force-with-lease
   ```

### Q: Can I version control `certs/`?
**A**: No, never commit private keys.

**Correct approach**:
- Dev: Generate with `generate-certs.ps1` (locally, not committed)
- Prod: Use Let's Encrypt + auto-renewal
- CI/CD: Mount from secrets manager or volume

### Q: Should `.env` (without extension) be committed?
**A**: No, it's a runtime file. Only `.env.dev` should be committed.

### Q: How do I handle multiple developers?
**A**: 

1. Each developer gets same `.env.dev` (from Git)
2. Each developer generates own `certs/` locally (ignored)
3. Production uses Jenkins secrets (not in Git)

---

## Summary

| Category | Files | Status |
|----------|-------|--------|
| **✅ Commit** | Jenkinsfile, docker-compose.prod.yml, .env.dev, nginx.conf, docs | Safe to share |
| **❌ Ignore** | .env.prod, certs/, jenkins_data/, secrets | Environment-specific |
| **Already Ignored** | appsettings.*.json, *.key, *.pem, .env.* (catch-all) | Protected by .gitignore |

**Golden Rule**: 
> 🔐 **NEVER commit anything that contains passwords, API keys, or production secrets.**

