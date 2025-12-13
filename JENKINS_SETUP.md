# Jenkins CI/CD Setup Guide

## Overview

This guide explains the Jenkins CI/CD pipeline configuration for HealthSync API, including:
- Automatic builds on code changes
- Docker image building
- Dev/Prod environment separation
- Automated deployment via docker-compose

## Architecture

```
┌──────────────────┐
│  GitHub / Git    │
│  (Repository)    │
└────────┬─────────┘
         │ Webhook
         v
┌──────────────────────────────┐
│  Jenkins Container           │
│  - Checkout code             │
│  - Build .NET solution       │
│  - Run unit tests            │
│  - Build Docker image        │
│  - Deploy via docker-compose │
└──────────────────────────────┘
         │
    ┌────┴────┐
    v         v
┌────────┐ ┌─────────┐
│  Dev   │ │  Prod   │
│ Stack  │ │ Stack   │
└────────┘ └─────────┘
```

## Files

| File | Purpose |
|------|---------|
| **Jenkinsfile** | Pipeline definition (stages: Checkout, Build, Test, Deploy) |
| **.env.dev** | Development environment variables |
| **.env.prod** | Production environment variables (with placeholders) |
| **docker-compose.yml** | Updated to use `.env.dev` and include Jenkins |
| **docker-compose.prod.yml** | Production compose file (uses `.env.prod`) |

## Quick Start

### 1. Start Jenkins

```powershell
# Jenkins is now part of docker-compose
docker-compose up -d jenkins

# Wait for Jenkins to initialize (2-3 minutes)
docker-compose logs -f jenkins

# Check Jenkins is ready
curl http://localhost:8081/
```

### 2. Access Jenkins UI

**URL**: http://localhost:8081

**First-time setup**:
1. Jenkins will ask for admin password
2. Find password in logs or:
   ```powershell
   docker-compose exec jenkins cat /var/jenkins_home/secrets/initialAdminPassword
   ```
3. Install suggested plugins (click "Install suggested plugins")
4. Create admin user
5. Configure Jenkins URL: http://localhost:8081

### 3. Install Required Plugins

**Required Plugins for Quality Analysis**:
1. **SonarQube Scanner** - For code quality analysis
2. **HTML Publisher** - For coverage reports
3. **JUnit** - For test results
4. **Quality Gates** - For SonarQube quality gate integration

**Installation**:
- Go to "Manage Jenkins" → "Manage Plugins"
- Search and install the above plugins
- Restart Jenkins after installation

### 4. Configure SonarQube Server

**Setup SonarQube**:
```bash
# Add SonarQube to docker-compose.yml
# (See docker-compose.yml for SonarQube service)

# Start SonarQube
docker-compose up -d sonarqube

# Access SonarQube: http://localhost:9000
# Default credentials: admin/admin
```

**In Jenkins**:
1. Go to "Manage Jenkins" → "Configure System"
2. Scroll to "SonarQube servers" section
3. Click "Add SonarQube"
4. Name: `SonarQube`
5. Server URL: `http://sonarqube:9000` (if using docker-compose) or `http://localhost:9000`
6. Server authentication token: Generate from SonarQube UI

### 5. Create a Pipeline Job

**Steps in Jenkins UI**:
1. Click "New Item"
2. Enter name: `HealthSync-CI-CD`
3. Select "Pipeline"
4. Click "OK"
5. In Configuration:
   - **Definition**: "Pipeline script from SCM"
   - **SCM**: Git
   - **Repository URL**: `https://github.com/strawberrymilktea0604/HealthSyncWebAPI.git`
   - **Credentials**: Add GitHub credentials (see below)
   - **Branch Specifier**: `*/main`
   - **Script Path**: `Jenkinsfile`
6. Click "Save"

### 4. Add GitHub Credentials

**In Jenkins UI**:
1. Go to "Manage Jenkins" → "Manage Credentials"
2. Click "global" domain
3. Click "Add Credentials"
4. Type: "Username with password"
   - Username: Your GitHub username
   - Password: GitHub Personal Access Token (PAT)
   - ID: `github-credentials`
5. Click "Create"

**Generate GitHub PAT**:
- Go to GitHub → Settings → Developer settings → Personal access tokens
- Click "Generate new token"
- Scopes: `repo`, `workflow`
- Save the token
- Use in Jenkins credentials

### 5. Trigger a Build

**Manual trigger**:
1. Open Jenkins job: `HealthSync-CI-CD`
2. Click "Build with Parameters"
3. Select Environment: `dev` or `prod`
4. Click "Build"

**Automatic trigger (Webhook)**:
1. In Jenkins job configuration:
   - Check "GitHub hook trigger for GITScm polling"
2. In GitHub repository:
   - Go to Settings → Webhooks
   - Click "Add webhook"
   - Payload URL: `http://YOUR_JENKINS_URL/github-webhook/`
   - Content type: `application/json`
   - Events: Push events, Pull requests
   - Click "Add webhook"

> Note: Webhook requires Jenkins to be publicly accessible. For local dev, use manual trigger.

## Environment Variables

### Development (.env.dev)

Used with `docker-compose.yml` for local development:

```properties
DB_SA_PASSWORD=CHANGE_ME
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=minioadmin
ASPNETCORE_ENVIRONMENT=Development
JWT_SECRET_KEY=dev-secret-key-min-32-characters-long-dev-only
```

### Production (.env.prod)

Used with `docker-compose.prod.yml` for production deployment:

```properties
DB_SA_PASSWORD=${PROD_DB_SA_PASSWORD}           # Set via CI/CD secrets
MINIO_ACCESS_KEY=${PROD_MINIO_ACCESS_KEY}       # Set via CI/CD secrets
MINIO_SECRET_KEY=${PROD_MINIO_SECRET_KEY}       # Set via CI/CD secrets
JWT_SECRET_KEY=${PROD_JWT_SECRET_KEY}           # Set via CI/CD secrets
GOOGLE_CLIENT_ID=${PROD_GOOGLE_CLIENT_ID}       # Set via CI/CD secrets
GOOGLE_CLIENT_SECRET=${PROD_GOOGLE_CLIENT_SECRET} # Set via CI/CD secrets
```

**Setting secrets in Jenkins**:
1. Go to Jenkins → Manage Jenkins → Configure System
2. Under "Global properties", add environment variables:
   - `PROD_DB_SA_PASSWORD`: Your prod DB password
   - `PROD_JWT_SECRET_KEY`: Your prod JWT secret (min 32 chars)
   - etc.
3. Click "Save"

Or use Jenkins Credentials Plugin:
1. Create credential of type "Secret text"
2. Use in Jenkinsfile: `withEnv(['PROD_JWT_SECRET_KEY=credentials("prod-jwt-secret")')`

## Jenkinsfile Stages

### Stage: Checkout
- Clones the repository from GitHub
- Uses credentials stored in Jenkins
- Checks out the specified branch (default: `main`)

### Stage: Prepare Environment
- Copies `.env.dev` or `.env.prod` based on selected environment
- Loads environment variables for subsequent stages

### Stage: Build Solution
- Runs `dotnet build` in Release mode
- Compiles all projects (.Domain, .Application, .Infrastructure, .WebApi)
- Fails if compilation errors

### Stage: Run Unit Tests
- Discovers `.Tests.csproj` files
- Runs unit tests with `dotnet test`
- Reports failures but continues pipeline (allows debugging)

### Stage: Build Docker Image
- Runs `docker build` with:
  - Tag: `{image}:{BUILD_NUMBER}-{ENVIRONMENT}`
  - Tag: `{image}:latest`
- Dockerfile must be in workspace root

### Stage: Push Docker Image
- **Dev**: Skips push (local testing only)
- **Prod**: Pushes to Docker registry (requires Docker credentials setup)

### Stage: Deploy Stack
- **Dev**: `docker-compose up -d` with `.env.dev`
- **Prod**: `docker-compose -f docker-compose.prod.yml up -d` with `.env.prod`
- Waits for services to initialize

### Stage: Health Check
- Polls API endpoint: `GET /health` (up to 30 times, 2-sec interval)
- Fails pipeline if health check doesn't pass
- Verifies deployment success

## Useful Jenkins Commands

```powershell
# View Jenkins logs
docker-compose logs -f jenkins

# Access Jenkins console
docker-compose exec jenkins /bin/sh

# Restart Jenkins
docker-compose restart jenkins

# Clear Jenkins data (WARNING: Removes jobs and config)
docker-compose down
docker volume rm healthsync_jenkins_data
docker-compose up -d jenkins
```

## Scaling & Advanced Setup

### Multiple Nodes/Agents

For distributed builds:
1. Configure Jenkins agents (slaves)
2. In Jenkinsfile: `agent { label 'docker-agent' }`
3. Agents must have Docker, .NET SDK installed

### Notifications

Add to Jenkinsfile `post` block:

```groovy
post {
    success {
        // Email, Slack, Teams notification
        slackSend(color: 'good', message: "Build #${BUILD_NUMBER} succeeded")
    }
    failure {
        slackSend(color: 'danger', message: "Build #${BUILD_NUMBER} failed")
    }
}
```

### SonarQube Integration

Add code quality scanning:

```groovy
stage('Code Quality') {
    steps {
        sh 'dotnet sonarscanner begin /k:healthsync /d:sonar.host.url=http://sonarqube:9000'
        sh 'dotnet build HealthSyncWebAPI.sln -c Release'
        sh 'dotnet sonarscanner end /d:sonar.login=${SONAR_TOKEN}'
    }
}
```

## Troubleshooting

### Jenkins Container Won't Start
```powershell
docker-compose logs jenkins | tail -50
# Check volume permissions, disk space
```

### Git Clone Fails
- Verify GitHub credentials are correct
- Check repository URL format
- Ensure SSH key or PAT has correct scopes

### Docker Build Fails
- Check Dockerfile exists in workspace root
- Verify Docker socket is mounted: `/var/run/docker.sock`
- Ensure Docker daemon is running

### Deployment Fails
- Check `.env.dev` or `.env.prod` file exists
- Verify environment variables are set correctly
- Check docker-compose syntax: `docker-compose config`
- View service logs: `docker-compose logs api`

### Health Check Times Out
- Verify API is running: `curl http://localhost:8080/health`
- Check API logs: `docker-compose logs api`
- Increase health check timeout in Jenkinsfile if needed

## Production Deployment Checklist

Before deploying to production:

- [ ] All secrets configured in Jenkins
- [ ] `.env.prod` file secure (not in git)
- [ ] SSL certificates valid (Let's Encrypt renewal)
- [ ] Database backups enabled
- [ ] MinIO credentials rotated
- [ ] Monitoring/alerting configured
- [ ] Disaster recovery plan tested
- [ ] Load testing completed
- [ ] Security audit passed
- [ ] Team trained on deployment process

## References

- Jenkins Official: https://www.jenkins.io/
- Jenkins Docker Image: https://hub.docker.com/r/jenkins/jenkins
- Jenkinsfile Syntax: https://www.jenkins.io/doc/book/pipeline/syntax/
- GitHub Personal Access Token: https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token
