#!/bin/bash
# Generate self-signed SSL certificates for development
# Usage: ./generate-certs.sh

CERTS_DIR="./certs"
mkdir -p $CERTS_DIR

# Check if certificates already exist
if [ -f "$CERTS_DIR/nginx.crt" ] && [ -f "$CERTS_DIR/nginx.key" ]; then
    echo "Certificates already exist in $CERTS_DIR"
    exit 0
fi

echo "Generating self-signed SSL certificates for development..."

# Generate private key (2048-bit RSA)
openssl genrsa -out $CERTS_DIR/nginx.key 2048

# Generate self-signed certificate (valid for 365 days)
openssl req -new -x509 -key $CERTS_DIR/nginx.key -out $CERTS_DIR/nginx.crt -days 365 \
    -subj "/C=VN/ST=HaNoi/L=HaNoi/O=HealthSync/CN=localhost"

echo "Certificates generated successfully in $CERTS_DIR"
echo "Certificate: $CERTS_DIR/nginx.crt"
echo "Key: $CERTS_DIR/nginx.key"
