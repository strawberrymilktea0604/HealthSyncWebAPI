# Generate self-signed SSL certificates for development on Windows
# Usage: .\generate-certs.ps1

$CertsDir = ".\certs"

# Create certs directory if it doesn't exist
if (-not (Test-Path $CertsDir)) {
    New-Item -ItemType Directory -Path $CertsDir | Out-Null
    Write-Host "Created directory: $CertsDir"
}

# Check if certificates already exist
if ((Test-Path "$CertsDir\nginx.crt") -and (Test-Path "$CertsDir\nginx.key")) {
    Write-Host "Certificates already exist in $CertsDir"
    exit 0
}

Write-Host "Generating self-signed SSL certificates for development..."

# Check if openssl is available
if (-not (Get-Command openssl -ErrorAction SilentlyContinue)) {
    Write-Host "Error: openssl command not found. Please install OpenSSL for Windows."
    Write-Host "Download from: https://slproweb.com/products/Win32OpenSSL.html"
    Write-Host "Or install via Chocolatey: choco install openssl"
    exit 1
}

# Generate private key (2048-bit RSA)
& openssl genrsa -out "$CertsDir\nginx.key" 2048

# Generate self-signed certificate (valid for 365 days)
& openssl req -new -x509 -key "$CertsDir\nginx.key" -out "$CertsDir\nginx.crt" -days 365 `
    -subj "/C=VN/ST=HaNoi/L=HaNoi/O=HealthSync/CN=localhost"

Write-Host "Certificates generated successfully in $CertsDir"
Write-Host "Certificate: $CertsDir\nginx.crt"
Write-Host "Key: $CertsDir\nginx.key"
