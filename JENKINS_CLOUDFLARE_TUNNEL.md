# Jenkins Pipeline - Cloudflare Quick Tunnel Integration

## Tổng quan

Jenkins pipeline đã được tích hợp để tự động lấy Cloudflare Quick Tunnel URLs sau khi deploy production thành công. URLs sẽ được:
- Hiển thị trong Jenkins console output
- Test tự động qua health check
- Lưu vào file `tunnel-urls.txt` trên production server

## Pipeline Stages

### 1. Checkout
Pull code từ GitHub repository

### 2. Prepare Secrets
Giải nén certificates cho NGINX HTTPS

### 3. Restore Dependencies
`dotnet restore` cho tất cả projects

### 4. Build Application
Build .NET solution với configuration Release

### 5. Run Unit Tests
Chạy xUnit tests với coverage report

### 6. SonarQube Analysis
Static code analysis và quality gates

### 7. Build Docker Image
Build Docker image cho API

### 8. Build & Push Nginx Image
Build và push NGINX reverse proxy image

### 9. Push Docker Image
Push images lên Docker Hub

### 10. Deploy to Production
- SSH vào production server
- Pull images mới
- Stop old containers
- Start new containers với `docker-compose.prod.yml`

### 11. Health Check
Kiểm tra NGINX health status và API `/health` endpoint

### 12. **Get Cloudflare Tunnel URLs** ⭐ (NEW)
Lấy Quick Tunnel URLs từ container logs:
- API URL (nginx tunnel)
- MinIO Files URL
- MinIO Console URL

## Cloudflare Quick Tunnel Stage

### Cách hoạt động:

```groovy
stage('Get Cloudflare Tunnel URLs') {
    steps {
        script {
            // 1. Wait 10 giây để tunnels khởi tạo
            sleep(10)
            
            // 2. SSH vào server và lấy URLs từ docker logs
            def apiUrl = sh(script: """
                ssh ... 'docker logs healthsync-tunnel-nginx 2>&1 | grep -oP "https://[^\\s]+\\.trycloudflare\\.com" | head -1'
            """, returnStdout: true).trim()
            
            // 3. Lưu vào environment variables
            env.TUNNEL_API_URL = apiUrl
            
            // 4. Test health check qua public URL
            curl ${apiUrl}/health
            
            // 5. Ghi URLs vào file trên server
            ssh ... 'echo "URLs..." > tunnel-urls.txt'
        }
    }
}
```

### Output mẫu:

```
========== STAGE: Get Cloudflare Tunnel URLs ==========
Waiting for Cloudflare Tunnels to initialize...
┌─ CLOUDFLARE QUICK TUNNEL URLS ──────────────────────────┐
│ API (nginx):      https://random-abc-123.trycloudflare.com
│ MinIO Files:      https://random-def-456.trycloudflare.com
│ MinIO Console:    https://random-ghi-789.trycloudflare.com
└──────────────────────────────────────────────────────────┘
Testing API via Cloudflare Tunnel...
✓ API is publicly accessible via Cloudflare Tunnel
```

## Post-Build Success Message

Pipeline success sẽ hiển thị URLs ngay trong Jenkins console:

```
========== BUILD: SUCCESS ==========
✓ Production pipeline completed successfully
Build: 42

========== CLOUDFLARE QUICK TUNNEL URLS ==========
🌐 API Endpoint:      https://random-abc-123.trycloudflare.com
🗄️  MinIO Storage:     https://random-def-456.trycloudflare.com
🎛️  MinIO Console:     https://random-ghi-789.trycloudflare.com

📊 Health Check:      https://random-abc-123.trycloudflare.com/health
📖 Swagger UI:        https://random-abc-123.trycloudflare.com/swagger
🔒 Admin Init:        POST https://random-abc-123.trycloudflare.com/api/v1/admin/initialize

⚠️  IMPORTANT: These URLs will change after restart!
📄 URLs saved to: /home/deploy/healthsync/tunnel-urls.txt on server
🔄 To get updated URLs: ssh into server and run ./get-tunnel-urls.ps1
====================================================
```

## File Output trên Production Server

File `tunnel-urls.txt` được tạo tự động:

```
┌────────────────────────────────────────────────────────────┐
│ Cloudflare Quick Tunnel URLs - HealthSync Production     │
│ Build: 42 - 2025-12-18 14:30:00                          │
├────────────────────────────────────────────────────────────┤
│ API (nginx):      https://random-abc-123.trycloudflare.com│
│ MinIO Files:      https://random-def-456.trycloudflare.com│
│ MinIO Console:    https://random-ghi-789.trycloudflare.com│
├────────────────────────────────────────────────────────────┤
│ 📊 Health Check:   https://random-abc-123.trycloudflare.com/health
│ 📖 Swagger UI:     https://random-abc-123.trycloudflare.com/swagger
│ 🔒 MinIO Console:  https://random-ghi-789.trycloudflare.com (admin/password)
└────────────────────────────────────────────────────────────┘

⚠️  Note: URLs will change after container restart!
🔄  To get updated URLs, run: ./get-tunnel-urls.ps1 -Environment prod
```

## Troubleshooting

### URLs hiển thị "NOT_READY"

**Nguyên nhân:** Cloudflare Tunnels chưa khởi tạo xong trong 10 giây

**Giải pháp:**
```bash
# SSH vào server
ssh user@prod-server -p 2222
cd /home/deploy/healthsync

# Check tunnel logs
docker logs healthsync-tunnel-nginx
docker logs healthsync-tunnel-minio
docker logs healthsync-tunnel-minio-console

# Lấy URLs bằng script
./get-tunnel-urls.ps1 -Environment prod
```

### Health check qua Tunnel failed

**Nguyên nhân:** Tunnel đã ready nhưng API chưa sẵn sàng

**Giải pháp:**
```bash
# Check API container status
docker ps --filter "name=api"

# Check API logs
docker logs healthsync-api-prod --tail 50

# Manual health check
curl https://random-abc-123.trycloudflare.com/health
```

### Tunnel URLs thay đổi giữa build

**Nguyên nhân:** Docker containers restart giữa pipeline

**Giải pháp:**
- URLs luôn được lấy ở stage cuối cùng (sau Health Check)
- File `tunnel-urls.txt` luôn được update với URLs mới nhất
- Kiểm tra Jenkins console output để lấy URLs chính xác

## Best Practices

### 1. Monitor Tunnel Health

Thêm vào crontab trên production server:

```bash
# Check tunnel health mỗi 5 phút
*/5 * * * * /home/deploy/healthsync/check-tunnels.sh
```

`check-tunnels.sh`:
```bash
#!/bin/bash
cd /home/deploy/healthsync
docker ps --filter "name=tunnel" --format "{{.Names}}: {{.Status}}" > tunnel-status.txt
```

### 2. Auto-notify Team về URLs mới

Thêm vào Jenkinsfile post success:

```groovy
// Send Slack notification
slackSend(
    channel: '#deployments',
    color: 'good',
    message: """
        ✅ HealthSync Production Deployed!
        Build: ${BUILD_NUMBER}
        API: ${env.TUNNEL_API_URL}
    """
)
```

### 3. Save URLs to External Storage

Upload `tunnel-urls.txt` lên S3/Dropbox:

```bash
# Sau khi tạo tunnel-urls.txt
aws s3 cp tunnel-urls.txt s3://healthsync-configs/tunnel-urls-${BUILD_NUMBER}.txt
```

## FAQ

**Q: Tại sao không dùng fixed URLs?**
A: Quick Tunnel miễn phí nhưng URLs thay đổi. Để có fixed URLs, cần:
- Mua domain ($1-10/year)
- Setup Named Tunnel với custom domain
- Xem: [CLOUDFLARE_TUNNEL_SETUP.md](./CLOUDFLARE_TUNNEL_SETUP.md#muốn-url-cố-định)

**Q: URLs có thay đổi giữa deployments không?**
A: Không, nếu containers không restart. URLs chỉ thay đổi khi:
- `docker-compose restart`
- `docker-compose down && up`
- Server reboot

**Q: Làm sao share URLs với testers?**
A: 
1. Lấy từ Jenkins console output (copy/paste)
2. SSH vào server: `cat /home/deploy/healthsync/tunnel-urls.txt`
3. Chạy script: `./get-tunnel-urls.ps1 -Environment prod`

**Q: API có thể gọi MinIO qua internal network không?**
A: Có! API dùng `http://minio:9000` (internal), không cần Quick Tunnel URLs. Quick Tunnel chỉ cho client bên ngoài.

## Next Steps

1. ✅ Pipeline tự động lấy URLs
2. ✅ URLs được lưu vào file trên server
3. ✅ Health check tự động qua Tunnel
4. ⚠️ (Optional) Setup Slack notification
5. ⚠️ (Optional) Upload URLs to S3
6. ⚠️ (Optional) Monitor tunnel uptime

## References

- [Jenkinsfile](./Jenkinsfile)
- [CLOUDFLARE_TUNNEL_SETUP.md](./CLOUDFLARE_TUNNEL_SETUP.md)
- [get-tunnel-urls.ps1](./get-tunnel-urls.ps1)
- [docker-compose.prod.yml](./docker-compose.prod.yml)
