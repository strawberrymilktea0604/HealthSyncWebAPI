# NGINX Reverse Proxy Setup

This guide explains the NGINX reverse proxy configuration for HealthSync API.

## Overview

NGINX acts as a reverse proxy in front of the ASP.NET Core API, providing:
- **SSL/TLS termination**: Handles HTTPS encryption/decryption
- **HTTP to HTTPS redirection**: Automatically redirects HTTP requests to HTTPS
- **Security headers**: Adds Strict-Transport-Security, X-Content-Type-Options, etc.
- **Load balancing**: Can be extended to support multiple API instances
- **Request proxying**: Routes requests to backend API with proper headers
- **File upload handling**: Configured for 5MB file uploads (adjustable via `client_max_body_size`)

## Architecture

```
Client (browser/curl)
   |
   v
NGINX Reverse Proxy (port 80, 443)
   |
   v
ASP.NET Core API (port 8080 internal)
```

## Files

1. **nginx.conf**: Main NGINX configuration file
   - Defines upstream backend (api:8080)
   - HTTP→HTTPS redirection
   - SSL/TLS settings
   - Security headers
   - Proxy headers and timeouts

2. **docker-compose.yml**: Updated with NGINX service
   - Image: `nginx:1.25-alpine`
   - Mounts `nginx.conf` and SSL certificates
   - Ports: 80 (HTTP), 443 (HTTPS)
   - Depends on API service

3. **generate-certs.sh**: Linux/macOS script to generate self-signed certificates

4. **generate-certs.ps1**: PowerShell script for Windows users

## Quick Start

### 1. Generate SSL Certificates (Development Only)

**On Windows (PowerShell):**
```powershell
.\generate-certs.ps1
```

**On Linux/macOS (Bash):**
```bash
chmod +x generate-certs.sh
./generate-certs.sh
```

This creates a `certs` directory with:
- `nginx.crt`: Self-signed certificate (valid 365 days)
- `nginx.key`: Private key

> Note: For production, use certificates from a trusted CA (Let's Encrypt, DigiCert, etc.)

### 2. Build and Run Docker Compose

```powershell
# Build and start all services (db, minio, api, nginx)
docker-compose up --build -d

# Check logs
docker-compose logs -f nginx

# Check service status
docker-compose ps
```

### 3. Access the API

**Via HTTPS (recommended):**
```powershell
# curl with self-signed cert skip (dev only)
curl -k https://localhost/api/v1/auth/login -H "Content-Type: application/json" -d '{"email":"test@example.com","password":"Test123!"}'

# Or with PowerShell
Invoke-RestMethod -Uri "https://localhost/api/v1/auth/login" -Method Post -SkipCertificateCheck -ContentType "application/json" -Body '{"email":"test@example.com","password":"Test123!"}'
```

**Via HTTP (redirects to HTTPS):**
```powershell
curl -L http://localhost/api/v1/auth/login
```

## Configuration Details

### nginx.conf Sections

1. **Upstream Definition**
   ```nginx
   upstream healthsync_api {
       server api:8080;
   }
   ```
   Points to the backend API container on internal port 8080.

2. **HTTP to HTTPS Redirect**
   ```nginx
   server {
       listen 80;
       return 301 https://$host$request_uri;
   }
   ```
   All HTTP requests redirect to HTTPS with 301 status.

3. **SSL/TLS Configuration**
   - Protocols: TLSv1.2, TLSv1.3 (modern standards)
   - Certificates: `/etc/nginx/certs/nginx.crt` and `nginx.key`
   - HSTS header: Forces HTTPS for 1 year

4. **Security Headers**
   - `Strict-Transport-Security`: Enforces HTTPS
   - `X-Content-Type-Options`: Prevents MIME sniffing
   - `X-Frame-Options`: Prevents clickjacking
   - `X-XSS-Protection`: Enables browser XSS filtering

5. **Proxy Headers**
   ```nginx
   proxy_set_header X-Real-IP $remote_addr;
   proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
   proxy_set_header X-Forwarded-Proto $scheme;
   ```
   These inform the backend API of the original client IP and protocol.

6. **Client Max Body Size**
   ```nginx
   client_max_body_size 5M;
   ```
   Allows up to 5MB file uploads (matches avatar upload requirements).

## Scaling to Multiple API Instances

To add load balancing, update `nginx.conf`:

```nginx
upstream healthsync_api {
    server api1:8080;
    server api2:8080;
    server api3:8080;
    # Least connections load balancing
    least_conn;
}
```

Then scale API services in `docker-compose.yml`:
```powershell
docker-compose up --scale api=3 -d
```

## Production Considerations

1. **SSL Certificates**: Replace self-signed certs with trusted CA certificates
   - Use Let's Encrypt (free): `certbot`
   - Or purchase from DigiCert, GoDaddy, etc.

2. **Update nginx.conf**
   - Change certificate paths to production certs
   - Add rate limiting for DDoS protection
   - Add caching rules for static assets (if any)

3. **Logging & Monitoring**
   - Check `access_log` and `error_log` in `/var/log/nginx/`
   - Monitor upstream backend health
   - Set up alerts for 5xx errors

4. **Performance Tuning**
   - Increase `worker_connections` for high traffic
   - Enable `gzip` compression for responses
   - Configure keepalive connections to backend

Example production snippet:
```nginx
# Enable gzip compression
gzip on;
gzip_types text/plain text/css application/json application/javascript;
gzip_min_length 1000;

# Keepalive to backend
upstream healthsync_api {
    server api:8080;
    keepalive 32;
}

# Rate limiting (10 requests per second)
limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;

server {
    location / {
        limit_req zone=api_limit burst=20 nodelay;
        proxy_pass http://healthsync_api;
        # ... other settings
    }
}
```

## Troubleshooting

### NGINX Container Not Starting
```powershell
docker-compose logs nginx
# Check for certificate file errors or nginx.conf syntax errors
```

### Connection Refused to Backend
```powershell
# Verify API container is running
docker-compose ps api

# Check network connectivity
docker exec healthsync-nginx ping api
```

### SSL Certificate Errors
```powershell
# Check certificate validity
openssl x509 -in certs/nginx.crt -text -noout

# Regenerate certificates
rm -r certs
.\generate-certs.ps1
docker-compose restart nginx
```

### Self-Signed Certificate Warning
- This is expected for development
- Use `-k` flag with curl or `-SkipCertificateCheck` in PowerShell
- Browsers will show "Not Secure" warning (safe to ignore in dev)

## Port Mappings

| Service | Internal Port | External Port | Protocol |
|---------|---------------|---------------|----------|
| NGINX   | 80            | 80 (HTTP)     | HTTP     |
| NGINX   | 443           | 443 (HTTPS)   | HTTPS    |
| API     | 8080          | (internal)    | HTTP     |
| DB      | 1433          | 1433          | T-SQL    |
| MinIO   | 9000          | 9000          | S3 API   |
| MinIO   | 9001          | 9001          | Web UI   |

## Version Information

- **NGINX**: `1.25-alpine`
  - Chosen for: Lightweight, Alpine Linux base (~10MB), stable LTS features, built-in SSL support
  - Alpine reduces image size vs full NGINX (~200MB)
  - Version 1.25 is stable with modern TLS/HTTP2 support

- **Alternative versions**:
  - `nginx:latest-alpine`: Follows latest releases (may break compatibility)
  - `nginx:1.26-alpine`: Newer (released mid-2024, recommended for new projects)
  - `nginx:ubuntu`: Heavier (~100MB), but more familiar for Ubuntu users

## References

- NGINX Official: https://nginx.org/
- Docker NGINX Image: https://hub.docker.com/_/nginx
- Let's Encrypt: https://letsencrypt.org/
- OWASP Security Headers: https://owasp.org/www-project-secure-headers/
