# Cloudflare Quick Tunnel Setup Guide

## Giới thiệu

Hướng dẫn này sẽ giúp bạn thiết lập **Cloudflare Quick Tunnel** để expose HealthSync API và các services ra Internet một cách cực kỳ đơn giản. Quick Tunnel **KHÔNG CẦN** domain, không cần config gì trên Cloudflare dashboard, chỉ cần chạy là có ngay URL public!

## Tại sao Cloudflare Quick Tunnel?

- ✅ **Miễn phí**: Hoàn toàn miễn phí, không giới hạn bandwidth
- ✅ **Cực kỳ đơn giản**: Không cần config, không cần token, không cần domain
- ✅ **Instant URL**: Tự động tạo URL dạng `https://random-name.trycloudflare.com`
- ✅ **An toàn**: Không cần mở port, tất cả traffic qua Cloudflare
- ✅ **DDoS Protection**: Tự động có Cloudflare DDoS protection
- ✅ **SSL/TLS**: Tự động HTTPS với Cloudflare SSL
- ⚠️ **URL thay đổi**: Mỗi lần restart, URL sẽ khác (random subdomain)

## Prerequisites

Chỉ cần **Docker & Docker Compose** đã cài đặt. Không cần gì khác!

## Bước 1: Deploy với Quick Tunnel

**KHÔNG CẦN CONFIG GÌ CẢ!** Chỉ cần chạy docker-compose là xong!

### 1.1. Production

```powershell
# Build và start
docker-compose -f docker-compose.prod.yml up -d --build

# Xem URLs được tạo tự động
docker logs healthsync-tunnel-nginx
docker logs healthsync-tunnel-minio
docker logs healthsync-tunnel-minio-console
```

### 1.2. Development

```powershell
# Build và start
docker-compose -f docker-compose.dev.yml up -d --build

# Xem URLs được tạo tự động
docker logs healthsync-tunnel-jenkins
docker logs healthsync-tunnel-sonarqube
```

## Bước 2: Lấy Public URLs

Cloudflare sẽ tự động tạo URLs dạng `https://random-name.trycloudflare.com`

### 2.1. Sử dụng Script Helper (Khuyên dùng)

```powershell
# Lấy tất cả URLs với format đẹp
.\get-tunnel-urls.ps1

# Hoặc chỉ lấy production URLs
.\get-tunnel-urls.ps1 -Environment prod

# Hoặc chỉ lấy development URLs
.\get-tunnel-urls.ps1 -Environment dev
```

Script sẽ hiển thị:
- ✅ URLs của tất cả tunnels đang chạy
- ⚠️ Cảnh báo nếu container chưa ready
- 🔍 Test API health endpoint tự động

### 2.2. Hoặc Check Logs thủ công

**Production:**

```powershell
# API URL
docker logs healthsync-tunnel-nginx 2>&1 | Select-String "trycloudflare.com"

# MinIO Files URL
docker logs healthsync-tunnel-minio 2>&1 | Select-String "trycloudflare.com"

# MinIO Console URL
docker logs healthsync-tunnel-minio-console 2>&1 | Select-String "trycloudflare.com"
```

**Development:**

```powershell
# Jenkins URL
docker logs healthsync-tunnel-jenkins 2>&1 | Select-String "trycloudflare.com"

# SonarQube URL
docker logs healthsync-tunnel-sonarqube 2>&1 | Select-String "trycloudflare.com"
```

### 2.2. Ví dụ Output

Script `get-tunnel-urls.ps1` sẽ hiển thị:

```
╔════════════════════════════════════════════════════════════╗
║        Cloudflare Quick Tunnel URLs - HealthSync         ║
╚════════════════════════════════════════════════════════════╝

┌─ PRODUCTION SERVICES ─────────────────────────────────────┐

API (nginx)
  ✅ https://random-abc-123.trycloudflare.com

MinIO Files
Sử dụng URLs từ script `get-tunnel-urls.ps1`:.trycloudflare.com

MinIO Console
  ✅ https://random-ghi-789.trycloudflare.com

└────────────────────────────────────────────────────────────┘

Testing API health...
  ✅ API is responding (Status: 200)

┌─ QUICK REFERENCE ─────────────────────────────────────────┐
│ Production:                                                │
│   API:           https://random-abc-123.trycloudflare.com │
│   MinIO Files:   https://random-def-456.trycloudflare.com │
│   MinIO Console: https://random-ghi-789.trycloudflare.com │
└────────────────────────────────────────────────────────────┘

💡 Tips:
   - URLs will change after each container restart
   - API uses internal 'minio:9000' for file operations
   - Only MinIO Console needs public URL for browser access
```

## Bước 3: Hiểu về MinIO URLs

### 3.1. Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ BẠCH: Browser/Client bên ngoài                             │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   │ ① Access API qua Quick Tunnel
                   │    https://random-abc-123.trycloudflare.com
                   ▼
┌─────────────────────────────────────────────────────────────┐
│ Cloudflare Quick Tunnel (tunnel-nginx)                     │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   │ ② Forward to nginx trong Docker network
                   │    http://nginx:80
                   ▼
┌─────────────────────────────────────────────────────────────┐
│ NGINX → API Container                                       │
│   ③ API gọi MinIO qua internal network                     │
│      http://minio:9000 (KHÔNG qua tunnel)                  │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   │ ④ Direct connection trong Docker network
                   ▼
┌─────────────────────────────────────────────────────────────┐
│ MinIO Server                                                │
│   - Port 9000: S3 API (file operations)                    │
│   - Port 9001: Web Console (management UI)                 │
└─────────────────────────────────────────────────────────────┘
```

### 3.2. Tại sao KHÔNG CẦN Quick Tunnel URLs cho MinIO?

**✅ API dùng internal network:**
- API container và MinIO container trong cùng Docker network
- Kết nối trực tiếp qua `http://minio:9000` (nhanh, không qua internet)
- Không cần lo URLs thay đổi khi restart

**⚠️ Quick Tunnel URLs chỉ dùng cho:**
- Browser truy cập MinIO Console UI từ bên ngoài
- Debug/testing trực tiếp MinIO S3 API từ client bên ngoài
- URLs này thay đổi sau mỗi restart

## Bước 4: Test Public Access

Sử dụng URLs vừa lấy được:

**Production:**
- API: `https://random-abc-123.trycloudflare.com/health`
- MinIO Files: `https://random-def-456.trycloudflare.com/minio/health/live`
- MinIO Console: `https://random-ghi-789.trycloudflare.com`

**Development:**
- Jenkins: `https://random-jkl-012.trycloudflare.com`
- SonarQube: `https://random-mno-345.trycloudflare.com`

### Test với script

Script `get-tunnel-urls.ps1` tự động test API health endpoint!

## Bước 5: Workflow hoàn chỉnh

### 5.1. Deploy lần đầu

```powershell
# 1. Build và start tất cả services
docker-compose -f docker-compose.prod.yml up -d --build

# 2. Đợi vài giây để tunnels khởi tạo
Start-Sleep -Seconds 10

# 3. Lấy URLs
.\get-tunnel-urls.ps1 -Environment prod

# 4. Test API
$apiUrl = docker logs healthsync-tunnel-nginx 2>&1 | Select-String "https://.*\.trycloudflare\.com" | Select-Object -First 1 | ForEach-Object { $_.Matches.Value }
Invoke-WebRequest -Uri "$apiUrl/health" -Method GET
```

### 5.2. Sau khi restart

```powershell
# 1. Restart services
docker-compose -f docker-compose.prod.yml restart

# 2. Đợi tunnels reconnect
Start-Sleep -Seconds 10

# 3. Lấy URLs mới (đã thay đổi!)
.\get-tunnel-urls.ps1 -Environment prod
```

### 5.3. Share URLs với team

```powershell
# Export URLs to file
.\get-tunnel-urls.ps1 -Environment prod | Out-File -FilePath tunnel-urls.txt

# Gửi file tunnel-urls.txt cho team
```

## Bước 6: Không cần update MinIO URLs!

**🎉 Tin tốt:** Bạn KHÔNG CẦN update MinIO URLs trong `.env`!

### Tại sao?

1. **API đã được config sẵn** để dùng internal URLs:
   ```yaml
   # docker-compose.prod.yml
   environment:
     - MinIO__Endpoint=minio:9000  # ← Internal network
   ```

2. **MinIO server cũng đã được config sẵn**:
   ```yaml
   # docker-compose.prod.yml
   environment:
     MINIO_SERVER_URL: http://minio:9000        # ← Internal
     MINIO_BROWSER_REDIRECT_URL: http://minio:9001
   ```

3. **Quick Tunnel URLs chỉ dùng để:**
   - Truy cập MinIO Console từ browser bên ngoài
   - Debug/test trực tiếp (không thông qua API)

### Khi nào cần Quick Tunnel MinIO URLs?

Chỉ khi bạn muốn:
- ✅ Login vào MinIO Console UI từ browser: `https://random-ghi-789.trycloudflare.com`
- ✅ Test upload/download trực tiếp qua S3 client tools
- ❌ KHÔNG CẦN cho API operations (API → MinIO là internal)

## Lưu ý quan trọng về Quick Tunnel

⚠️ **URLs sẽ thay đổi mỗi lần restart:**
- Quick Tunnel tạo random subdomain mỗi lần khởi động
- Không phù hợp cho production long-term (dùng cho demo/testing)
- Nếu cần fixed URLs, hãy dùng Named Tunnels với custom domain

✅ **Ưu điểm:**
- Cực kỳ đơn giản, không cần config
- Không cần domain
- Miễn phí, unlimited bandwidth
- Tự động HTTPS

❌ **Nhược điểm:**
- URLs thay đổi sau mỗi restart
- Không có Access Control
- Không có custom branding

## Troubleshooting

### Tunnel không connect

**Kiểm tra:**
1. Container có running không? `docker ps --filter "name=tunnel"`
2. Logs có error gì? `docker logs healthsync-tunnel-nginx`
3. Service backend có ready không?

**Giải pháp:**
```powershell
# Restart tunnel
docker restart healthsync-tunnel-nginx

# Rebuild nếu cần
docker-compose -f docker-compose.prod.yml up -d --build tunnel-nginx
```

### 502 Bad Gateway

**Nguyên nhân:** Service backend chưa ready hoặc URL config sai

**Kiểm tra:**
1. Service backend có running không?
   ```powershell
   docker ps --filter "name=nginx"
   docker ps --filter "name=minio"
   ```
2. Health check có pass không?
   ```powershell
   docker inspect healthsync-nginx-prod | grep -A 10 Health
   ```

**Giải pháp:**
1. Đảm bảo service backend đã start: `docker-compose -f docker-compose.prod.yml up -d`
2. Restart tunnel: `docker restart healthsync-tunnel-nginx`
3. Check logs để xem URL mới: `docker logs healthsync-tunnel-nginx`

### Làm sao để lấy URL sau khi restart?

**PowerShell command:**
```powershell
# Lấy tất cả URLs một lần
docker logs healthsync-tunnel-nginx 2>&1 | Select-String "https://.*\.trycloudflare\.com"
docker logs healthsync-tunnel-minio 2>&1 | Select-String "https://.*\.trycloudflare\.com"
docker logs healthsync-tunnel-minio-console 2>&1 | Select-String "https://.*\.trycloudflare\.com"
docker logs healthsync-tunnel-jenkins 2>&1 | Select-String "https://.*\.trycloudflare\.com"
docker logs healthsync-tunnel-sonarqube 2>&1 | Select-String "https://.*\.trycloudflare\.com"
```

### Muốn URL cố định?

Quick Tunnel không hỗ trợ fixed URLs. Nếu cần:
1. **Mua domain** (từ $1/năm trên Namecheap, GoDaddy)
2. **Add domain vào Cloudflare** (miễn phí)
3. **Tạo Named Tunnel** với custom domain
4. Xem hướng dẫn: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/

## So sánh Tunneling Solutions

| Feature | Cloudflare Quick Tunnel | Named Tunnel | Ngrok | Loophole |
|---------|------------------------|--------------|-------|----------|
| **Setup** | Cực đơn giản | Cần config domain | Đơn giản | Đơn giản |
| **Domain** | Random | Custom | Custom (paid) | Custom (paid) |
| **URLs** | Thay đổi | Fixed | Fixed | Fixed |
| **Bandwidth** | Unlimited | Unlimited | Limited (free) | Limited |
| **Giá** | Miễn phí | Miễn phí | $8/month | Free (limited) |
| **DDoS Protection** | ✅ Cloudflare | ✅ Cloudflare | ✅ | ❌ |
| **SSL/TLS** | ✅ Auto | ✅ Auto | ✅ Auto | ✅ |
| **Access Control** | ❌ | ✅ | ✅ (paid) | ❌ |

## Scripts Tiện ích

### Lấy tất cả URLs một lần (Production)

```powershell
Write-Host "`n=== PRODUCTION TUNNELS ===" -ForegroundColor Green
Write-Host "`nAPI (nginx):" -ForegroundColor Yellow
docker logs healthsync-tunnel-nginx 2>&1 | Select-String "https://.*\.trycloudflare\.com" | Select-Object -First 1

Write-Host "`nMinIO Files:" -ForegroundColor Yellow
docker logs healthsync-tunnel-minio 2>&1 | Select-String "https://.*\.trycloudflare\.com" | Select-Object -First 1

Write-Host "`nMinIO Console:" -ForegroundColor Yellow
docker logs healthsync-tunnel-minio-console 2>&1 | Select-String "https://.*\.trycloudflare\.com" | Select-Object -First 1
```

### Lấy tất cả URLs một lần (Development)

```powershell
Write-Host "`n=== DEVELOPMENT TUNNELS ===" -ForegroundColor Green
Write-Host "`nJenkins:" -ForegroundColor Yellow
docker logs healthsync-tunnel-jenkins 2>&1 | Select-String "https://.*\.trycloudflare\.com" | Select-Object -First 1

Write-Host "`nSonarQube:" -ForegroundColor Yellow
docker logs healthsync-tunnel-sonarqube 2>&1 | Select-String "https://.*\.trycloudflare\.com" | Select-Object -First 1
```Deploy: `docker-compose -f docker-compose.prod.yml up -d --build`
2. ✅ Lấy URLs từ logs (xem Scripts Tiện ích phía trên)
3. ✅ Test public access qua browser
4. ✅ Share URLs với team/testers
5. ⚠️ Lưu ý: URLs sẽ thay đổi sau mỗi restart!

## References

- Cloudflare Quick Tunnel: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/do-more-with-tunnels/trycloudflare/
- Cloudflare Tunnel Documentation: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/
- Cloudflare Community: https://community.cloudflare.com/
## Next Steps

1. ✅ Setup Cloudflare Tunnel tokens trong `.env` files
2. ✅ Deploy production: `docker-compose -f docker-compose.prod.yml up -d --build`
3. ✅ Deploy development: `docker-compose -f docker-compose.dev.yml up -d --build`
4. ✅ Verify public access qua browser
5. ⚠️ (Optional) Enable Cloudflare Access để restrict access
6. ⚠️ (Optional) Setup WAF rules cho security
7. ⚠️ (Optional) Setup Rate Limiting

## References

- Cloudflare Tunnel Documentation: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/
- Zero Trust Dashboard: https://one.dash.cloudflare.com/
- Cloudflare Community: https://community.cloudflare.com/

## Support

Nếu gặp vấn đề:
1. Check logs: `docker logs <container_name>`
2. Check Cloudflare dashboard: https://dash.cloudflare.com/
3. Tham khảo Troubleshooting section ở trên
4. Liên hệ Cloudflare Support (nếu cần)
