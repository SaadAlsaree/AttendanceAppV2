# دليل اختبار الحماية الأمنية - Security Testing Guide

## 🧪 اختبار شامل لجميع المكونات الأمنية

### 1. اختبار Rate Limiting Middleware

#### اختبار أساسي:

```bash
# اختبار تجاوز الحد (60 طلب/دقيقة)
for i in {1..70}; do
    echo "Request $i"
    curl -w "Status: %{http_code}\n" http://localhost:5000/api/health
    sleep 0.5
done
```

#### اختبار متقدم بـ PowerShell:

```powershell
# اختبار Rate Limiting
for ($i = 1; $i -le 70; $i++) {
    Write-Host "Request $i"
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/health" -ErrorAction SilentlyContinue
    Write-Host "Status: $($response.StatusCode)"
    Start-Sleep -Milliseconds 500
}
```

#### النتيجة المتوقعة:

-  الطلبات 1-60: `200 OK`
-  الطلبات 61+: `429 Too Many Requests`

---

### 2. اختبار SQL Injection Protection

#### اختبارات مختلفة:

```bash
# اختبار 1: Union-based injection
curl "http://localhost:5000/api/test?id=1' UNION SELECT * FROM users--"

# اختبار 2: Boolean-based injection
curl "http://localhost:5000/api/test?id=1' OR '1'='1"

# اختبار 3: Time-based injection
curl "http://localhost:5000/api/test?id=1'; WAITFOR DELAY '00:00:10'--"

# اختبار 4: Error-based injection
curl "http://localhost:5000/api/test?id=1' AND (SELECT COUNT(*) FROM information_schema.tables)>0--"
```

#### اختبار مع POST data:

```bash
# اختبار SQL injection في JSON body
curl -X POST http://localhost:5000/api/test \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "test'\'' OR '\''1'\''='\''1"}'
```

#### النتيجة المتوقعة:

-  جميع الطلبات: `403 Forbidden`
-  رسالة: "SQL injection attempt detected and blocked"

---

### 3. اختبار XSS Protection

#### اختبارات مختلفة:

```bash
# اختبار 1: Script tag
curl -X POST http://localhost:5000/api/test \
  -H "Content-Type: application/json" \
  -d '{"content": "<script>alert('\''xss'\'')</script>"}'

# اختبار 2: Event handler
curl -X POST http://localhost:5000/api/test \
  -d "input=<img src=x onerror=alert('xss')>"

# اختبار 3: JavaScript URL
curl -X POST http://localhost:5000/api/test \
  -d "link=javascript:alert('xss')"
```

#### النتيجة المتوقعة:

-  `400 Bad Request`
-  رسالة: "Suspicious request content detected"

---

### 4. اختبار IP Blocking

#### إنشاء نشاط مشبوه:

```bash
# إرسال طلبات مشبوهة متعددة
for i in {1..15}; do
    echo "Suspicious request $i"
    curl "http://localhost:5000/admin/config?cmd=whoami"
    curl "http://localhost:5000/../etc/passwd"
    curl -H "User-Agent: sqlmap/1.0" http://localhost:5000/api/test
    sleep 1
done
```

#### النتيجة المتوقعة:

-  أول 10 طلبات: `403 Forbidden`
-  بعد 10 محاولات: IP يتم حظره تماماً

---

### 5. اختبار CSRF Protection

#### اختبار بدون token:

```bash
# POST request بدون CSRF token
curl -X POST http://localhost:5000/api/test \
  -H "Content-Type: application/json" \
  -d '{"data": "test"}'
```

#### اختبار مع token خاطئ:

```bash
# POST request مع token خاطئ
curl -X POST http://localhost:5000/api/test \
  -H "Content-Type: application/json" \
  -H "X-CSRF-Token: invalid-token" \
  -d '{"data": "test"}'
```

#### الحصول على token صحيح:

```bash
# 1. احصل على token من GET request
curl -v http://localhost:5000/api/test 2>&1 | grep "X-CSRF-Token"

# 2. استخدم الـ token في POST request
curl -X POST http://localhost:5000/api/test \
  -H "Content-Type: application/json" \
  -H "X-CSRF-Token: [TOKEN_HERE]" \
  -b "cookies.txt" \
  -d '{"data": "test"}'
```

#### النتيجة المتوقعة:

-  بدون token: `403 Forbidden`
-  مع token صحيح: `200 OK`

---

### 6. اختبار Path Traversal Protection

#### اختبارات مختلفة:

```bash
# اختبار 1: Basic path traversal
curl "http://localhost:5000/api/files?path=../../../etc/passwd"

# اختبار 2: URL encoded
curl "http://localhost:5000/api/files?path=%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd"

# اختبار 3: Double encoding
curl "http://localhost:5000/api/files?path=%252e%252e%252f%252e%252e%252f"
```

#### النتيجة المتوقعة:

-  `400 Bad Request`
-  رسالة: "Suspicious request content detected"

---

### 7. اختبار Command Injection Protection

#### اختبارات مختلفة:

```bash
# اختبار 1: Command injection
curl -X POST http://localhost:5000/api/test \
  -d "command=ls; cat /etc/passwd"

# اختبار 2: Pipe commands
curl -X POST http://localhost:5000/api/test \
  -d "input=test | whoami"

# اختبار 3: Backticks
curl -X POST http://localhost:5000/api/test \
  -d "data=`id`"
```

#### النتيجة المتوقعة:

-  `400 Bad Request`
-  رسالة: "Suspicious request content detected"

---

### 8. اختبار Security Headers

#### فحص Headers:

```bash
# فحص جميع الـ security headers
curl -I http://localhost:5000/api/test
```

#### النتيجة المتوقعة:

```
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline'...
Referrer-Policy: strict-origin-when-cross-origin
```

---

### 9. اختبار File Upload Security

#### اختبار رفع ملفات خطيرة:

```bash
# إنشاء ملف خطير
echo "<?php system(\$_GET['cmd']); ?>" > malicious.php

# محاولة رفع الملف
curl -X POST http://localhost:5000/api/upload \
  -F "file=@malicious.php"

# اختبار أنواع ملفات أخرى خطيرة
echo "malicious content" > test.exe
curl -X POST http://localhost:5000/api/upload \
  -F "file=@test.exe"
```

#### النتيجة المتوقعة:

-  `400 Bad Request`
-  رسالة: "Dangerous file upload detected"

---

## 🔧 أدوات اختبار متقدمة

### استخدام PowerShell لاختبار شامل:

```powershell
# سكريبت اختبار شامل
function Test-SecurityMiddleware {
    param(
        [string]$BaseUrl = "http://localhost:5000"
    )

    Write-Host "🧪 بدء اختبار الحماية الأمنية..." -ForegroundColor Green

    # اختبار Rate Limiting
    Write-Host "`n1. اختبار Rate Limiting..." -ForegroundColor Yellow
    for ($i = 1; $i -le 70; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "$BaseUrl/api/health" -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 429) {
                Write-Host "✅ Rate limiting works! Request $i blocked" -ForegroundColor Green
                break
            }
        } catch {
            if ($_.Exception.Response.StatusCode -eq 429) {
                Write-Host "✅ Rate limiting works! Request $i blocked" -ForegroundColor Green
                break
            }
        }
        Start-Sleep -Milliseconds 100
    }

    # اختبار SQL Injection
    Write-Host "`n2. اختبار SQL Injection Protection..." -ForegroundColor Yellow
    $sqlPayloads = @(
        "1' OR '1'='1",
        "1'; DROP TABLE users--",
        "1' UNION SELECT * FROM information_schema.tables--"
    )

    foreach ($payload in $sqlPayloads) {
        try {
            $response = Invoke-WebRequest -Uri "$BaseUrl/api/test?id=$payload" -ErrorAction SilentlyContinue
        } catch {
            if ($_.Exception.Response.StatusCode -eq 403) {
                Write-Host "✅ SQL Injection blocked: $payload" -ForegroundColor Green
            }
        }
    }

    # اختبار XSS
    Write-Host "`n3. اختبار XSS Protection..." -ForegroundColor Yellow
    $xssPayloads = @(
        "<script>alert('xss')</script>",
        "<img src=x onerror=alert('xss')>",
        "javascript:alert('xss')"
    )

    foreach ($payload in $xssPayloads) {
        try {
            $body = @{ content = $payload } | ConvertTo-Json
            $response = Invoke-WebRequest -Uri "$BaseUrl/api/test" -Method POST -Body $body -ContentType "application/json" -ErrorAction SilentlyContinue
        } catch {
            if ($_.Exception.Response.StatusCode -eq 400) {
                Write-Host "✅ XSS blocked: $($payload.Substring(0, 20))..." -ForegroundColor Green
            }
        }
    }

    Write-Host "`n🎉 اختبار الحماية مكتمل!" -ForegroundColor Green
}

# تشغيل الاختبار
Test-SecurityMiddleware
```

### استخدام curl script:

```bash
#!/bin/bash
# security_test.sh

BASE_URL="http://localhost:5000"

echo "🧪 بدء اختبار الحماية الأمنية..."

# اختبار Rate Limiting
echo -e "\n1. اختبار Rate Limiting..."
for i in {1..70}; do
    status=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/health")
    if [ "$status" = "429" ]; then
        echo "✅ Rate limiting works! Request $i blocked"
        break
    fi
    sleep 0.1
done

# اختبار SQL Injection
echo -e "\n2. اختبار SQL Injection Protection..."
sql_payloads=(
    "1' OR '1'='1"
    "1'; DROP TABLE users--"
    "1' UNION SELECT * FROM information_schema.tables--"
)

for payload in "${sql_payloads[@]}"; do
    status=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/test?id=$payload")
    if [ "$status" = "403" ]; then
        echo "✅ SQL Injection blocked: $payload"
    fi
done

# اختبار XSS
echo -e "\n3. اختبار XSS Protection..."
xss_payloads=(
    "<script>alert('xss')</script>"
    "<img src=x onerror=alert('xss')>"
    "javascript:alert('xss')"
)

for payload in "${xss_payloads[@]}"; do
    status=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/test" \
        -H "Content-Type: application/json" \
        -d "{\"content\": \"$payload\"}")
    if [ "$status" = "400" ]; then
        echo "✅ XSS blocked: ${payload:0:20}..."
    fi
done

echo -e "\n🎉 اختبار الحماية مكتمل!"
```

---

## 📊 مراقبة النتائج

### فحص السجلات:

```bash
# فحص سجلات الأمان
tail -f logs/app-*.log | grep -E "(Warning|Error)"

# أو باستخدام PowerShell
Get-Content "logs\app-*.log" -Wait | Where-Object { $_ -match "(Warning|Error)" }
```

### السجلات المتوقعة:

```log
[Warning] Rate limit exceeded for client: 127.0.0.1
[Error] SQL injection attempt detected from IP: 127.0.0.1
[Warning] Suspicious User-Agent detected: curl/7.68.0 from 127.0.0.1
[Error] CSRF attack attempt detected from IP: 127.0.0.1
```

---

## ✅ قائمة التحقق النهائية

-  [ ] Rate Limiting: يحظر بعد 60 طلب/دقيقة
-  [ ] SQL Injection: يكشف ويحظر جميع الأنماط
-  [ ] XSS Protection: يكشف ويحظر محتوى ضار
-  [ ] CSRF Protection: يتطلب token صحيح
-  [ ] Path Traversal: يمنع الوصول للمجلدات
-  [ ] Command Injection: يكشف محاولات تنفيذ أوامر
-  [ ] IP Blocking: يحظر IPs مشبوهة
-  [ ] File Upload: يمنع رفع ملفات خطيرة
-  [ ] Security Headers: جميع الـ headers موجودة
-  [ ] Security Logging: يسجل جميع الأحداث

---

> 💡 **نصيحة**: قم بتشغيل هذه الاختبارات في بيئة التطوير أولاً للتأكد من عمل جميع المكونات بشكل صحيح قبل النشر في الإنتاج.
