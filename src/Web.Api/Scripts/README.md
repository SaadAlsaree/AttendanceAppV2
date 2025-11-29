# سكريبتات اختبار الحماية الأمنية

هذا المجلد يحتوي على سكريبتات لاختبار جميع مكونات الحماية الأمنية بشكل تلقائي.

## 📋 المحتويات

### 1. `Test-SecurityMiddleware.ps1` (PowerShell)

سكريبت PowerShell لنظام Windows لاختبار جميع مكونات الحماية.

#### الاستخدام:

```powershell
# اختبار أساسي
.\Test-SecurityMiddleware.ps1

# اختبار مع تفاصيل إضافية
.\Test-SecurityMiddleware.ps1 -Verbose

# اختبار مع URL مخصص
.\Test-SecurityMiddleware.ps1 -BaseUrl "http://localhost:8080" -Verbose
```

### 2. `test-security.sh` (Bash)

سكريبت Bash لأنظمة Linux/macOS لاختبار جميع مكونات الحماية.

#### المتطلبات:

-  `curl` - لإرسال HTTP requests
-  `jq` - لمعالجة JSON

#### التثبيت على Ubuntu/Debian:

```bash
sudo apt update
sudo apt install curl jq
```

#### التثبيت على macOS:

```bash
brew install curl jq
```

#### الاستخدام:

```bash
# جعل السكريبت قابل للتنفيذ
chmod +x test-security.sh

# اختبار أساسي
./test-security.sh

# اختبار مع تفاصيل إضافية
./test-security.sh --verbose

# اختبار مع URL مخصص
./test-security.sh --url "http://localhost:8080" --verbose

# عرض المساعدة
./test-security.sh --help
```

## 🧪 الاختبارات المتاحة

### 1. Rate Limiting Test

-  **الهدف**: اختبار حدود الطلبات المسموحة
-  **الطريقة**: إرسال 80 طلب متتالي
-  **النتيجة المتوقعة**: حظر بعد 60 طلب

### 2. SQL Injection Protection

-  **الهدف**: اختبار الحماية من حقن قواعد البيانات
-  **الطريقة**: إرسال payloads مختلفة
-  **النتيجة المتوقعة**: حظر جميع المحاولات (403)

### 3. XSS Protection

-  **الهدف**: اختبار الحماية من البرمجة النصية المتقاطعة
-  **الطريقة**: إرسال محتوى ضار
-  **النتيجة المتوقعة**: حظر جميع المحاولات (400)

### 4. Path Traversal Protection

-  **الهدف**: اختبار الحماية من اختراق المجلدات
-  **الطريقة**: محاولة الوصول لملفات النظام
-  **النتيجة المتوقعة**: حظر جميع المحاولات (400)

### 5. Command Injection Protection

-  **الهدف**: اختبار الحماية من حقن الأوامر
-  **الطريقة**: إرسال أوامر ضارة
-  **النتيجة المتوقعة**: حظر جميع المحاولات (400)

### 6. Security Headers Check

-  **الهدف**: التحقق من وجود Headers الأمنية
-  **الطريقة**: فحص response headers
-  **النتيجة المتوقعة**: وجود جميع الـ headers المطلوبة

### 7. File Upload Security

-  **الهدف**: اختبار أمان رفع الملفات
-  **الطريقة**: محاولة رفع ملفات خطيرة
-  **النتيجة المتوقعة**: حظر الملفات الخطيرة

### 8. Suspicious Activity Detection

-  **الهدف**: اختبار كشف الأنشطة المشبوهة
-  **الطريقة**: استخدام User-Agents مشبوهة
-  **النتيجة المتوقعة**: كشف وتسجيل الأنشطة

## 📊 نتائج الاختبار

### نتائج ناجحة:

```
✅ PASS - Rate Limiting Detection
✅ PASS - SQL Injection Protection
✅ PASS - XSS Protection
✅ PASS - Path Traversal Protection
✅ PASS - Command Injection Protection
✅ PASS - Security Headers
✅ PASS - File Upload Security
✅ PASS - Suspicious User-Agent Detection
```

### نتائج فاشلة:

```
❌ FAIL - Rate Limiting Detection
❌ FAIL - SQL Injection Protection
```

## 🔧 استكشاف الأخطاء

### المشاكل الشائعة:

#### 1. "Application is not accessible"

```bash
# تحقق من تشغيل التطبيق
curl http://localhost:5000/api/security-test/health

# تحقق من البورت
netstat -an | grep 5000
```

#### 2. "curl: command not found" (Linux/macOS)

```bash
# Ubuntu/Debian
sudo apt install curl

# CentOS/RHEL
sudo yum install curl

# macOS
brew install curl
```

#### 3. "jq: command not found" (Linux/macOS)

```bash
# Ubuntu/Debian
sudo apt install jq

# CentOS/RHEL
sudo yum install jq

# macOS
brew install jq
```

#### 4. إذن منفوض للسكريبت (Linux/macOS)

```bash
chmod +x test-security.sh
```

## 📝 مراقبة السجلات

### أثناء تشغيل الاختبارات:

#### Windows (PowerShell):

```powershell
# مراقبة السجلات مباشرة
Get-Content "logs\app-*.log" -Wait | Where-Object { $_ -match "(Warning|Error)" }

# فحص آخر 50 سطر
Get-Content "logs\app-*.log" -Tail 50 | Where-Object { $_ -match "(Warning|Error)" }
```

#### Linux/macOS:

```bash
# مراقبة السجلات مباشرة
tail -f logs/app-*.log | grep -E "(Warning|Error)"

# فحص آخر 50 سطر
tail -50 logs/app-*.log | grep -E "(Warning|Error)"
```

### أمثلة على السجلات المتوقعة:

```log
[Warning] Rate limit exceeded for client: 127.0.0.1
[Error] SQL injection attempt detected from IP: 127.0.0.1
[Warning] Suspicious User-Agent detected: sqlmap/1.0 from 127.0.0.1
[Error] XSS attack pattern detected in request body from IP: 127.0.0.1
[Warning] Path traversal pattern detected: ../../../etc/passwd from IP: 127.0.0.1
```

## 🚀 الاستخدام في CI/CD

### مع GitHub Actions:

```yaml
- name: Test Security Middleware
  run: |
     chmod +x Scripts/test-security.sh
     ./Scripts/test-security.sh --url "http://localhost:5000"
```

### مع Docker:

```bash
# تشغيل التطبيق
docker-compose up -d

# انتظار حتى يصبح التطبيق جاهز
sleep 30

# تشغيل الاختبارات
./Scripts/test-security.sh

# إيقاف التطبيق
docker-compose down
```

## 💡 نصائح إضافية

1. **تشغيل منتظم**: نفذ هذه الاختبارات بانتظام للتأكد من عمل الحماية
2. **بيئة منفصلة**: استخدم بيئة تطوير منفصلة للاختبار
3. **مراقبة الأداء**: راقب تأثير الاختبارات على أداء التطبيق
4. **توثيق النتائج**: سجل نتائج الاختبارات للمراجعة المستقبلية
5. **تحديث الاختبارات**: حدث الاختبارات عند إضافة ميزات أمنية جديدة

---

> ⚠️ **تنبيه**: هذه الاختبارات مصممة لبيئة التطوير. لا تستخدمها في بيئة الإنتاج بدون إذن مسبق.
