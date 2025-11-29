# دليل الحماية الأمنية - Security Guide

## نظرة عامة

تم إنشاء مجموعة شاملة من middleware للحماية من الهجمات المختلفة وتأمين التطبيق ضد:

-  ✅ **DDoS Attacks** - هجمات رفض الخدمة
-  ✅ **SQL Injection** - حقن قواعد البيانات
-  ✅ **XSS Attacks** - البرمجة النصية المتقاطعة
-  ✅ **CSRF Attacks** - تزوير الطلبات عبر المواقع
-  ✅ **Path Traversal** - اختراق المجلدات
-  ✅ **Command Injection** - حقن الأوامر
-  ✅ **IP-based Attacks** - الهجمات المعتمدة على عنوان IP
-  ✅ **Malicious File Uploads** - رفع الملفات الضارة
-  ✅ **Information Disclosure** - تسريب المعلومات

## مكونات الحماية

### 1. Rate Limiting Protection

```csharp
// تحديد عدد الطلبات لمنع DDoS
- 60 طلب/دقيقة لكل IP
- 1000 طلب/ساعة لكل IP
- حظر مؤقت 15 دقيقة عند التجاوز
```

### 2. IP Blocking System

```csharp
// نظام حظر عناوين IP المشبوهة
- كشف تلقائي للأنشطة المشبوهة
- حظر بعد 10 محاولات مشبوهة
- قائمة بيضاء للـ IPs الموثوقة
- حظر لمدة 24 ساعة
```

### 3. Advanced Request Validation

```csharp
// فحص شامل للطلبات
- كشف SQL injection patterns
- كشف XSS attacks
- كشف Path traversal
- كشف Command injection
- فحص رفع الملفات
- تحديد حجم الطلبات
```

### 4. SQL Injection Protection

```csharp
// حماية متقدمة من SQL injection
- Union-based injection detection
- Boolean-based blind injection
- Time-based blind injection
- Error-based injection
- Information schema access detection
- فحص JSON/Form data
```

### 5. Security Headers

```csharp
// Headers أمنية شاملة
- X-Frame-Options: DENY
- X-Content-Type-Options: nosniff
- Strict-Transport-Security
- Content-Security-Policy
- Referrer-Policy
- Permissions-Policy
```

### 6. CSRF Protection

```csharp
// حماية من تزوير الطلبات
- Token-based protection
- Secure cookie storage
- Header validation
- Form field validation
```

### 7. Security Logging

```csharp
// تسجيل أمني متقدم
- مراقبة الطلبات المشبوهة
- تسجيل محاولات الهجمات
- معلومات جغرافية للـ IPs
- تسجيل فشل المصادقة
```

## التفعيل السريع

### في `Program.cs`:

```csharp
// إضافة جميع المكونات الأمنية
app.UseSecurityMiddleware();
```

### أو تفعيل مكونات محددة:

```csharp
app.UseSecurityHeaders();
app.UseSecurityLogging();
app.UseRateLimiting();
app.UseIpBlocking();
app.UseCsrfProtection();
app.UseRequestValidation();
app.UseSqlInjectionProtection();
```

## التكوين في appsettings.json

```json
{
   "Security": {
      "RateLimiting": {
         "MaxRequestsPerMinute": 60,
         "MaxRequestsPerHour": 1000,
         "BlockDurationMinutes": 15,
         "Enabled": true
      },
      "IpBlocking": {
         "MaxFailedAttemptsBeforeBlock": 10,
         "SuspiciousActivityWindowMinutes": 15,
         "BlockDurationHours": 24,
         "Enabled": true,
         "WhitelistedIps": ["127.0.0.1", "::1", "192.168.1.0/24"]
      },
      "RequestValidation": {
         "MaxRequestSize": 10485760,
         "MaxHeaderLength": 8192,
         "MaxUrlLength": 2048,
         "Enabled": true,
         "ExcludedPaths": ["/health", "/swagger"]
      }
   }
}
```

## حالات الاستجابة

| كود الحالة | السبب         | الوصف                       |
| ---------- | ------------- | --------------------------- |
| `403`      | IP محظور      | عنوان IP في القائمة السوداء |
| `403`      | CSRF          | رمز CSRF مفقود أو خاطئ      |
| `403`      | SQL Injection | محاولة حقن قاعدة بيانات     |
| `400`      | طلب خاطئ      | طلب يحتوي على محتوى ضار     |
| `429`      | كثرة الطلبات  | تجاوز حد الطلبات المسموح    |

## مراقبة السجلات

### أنواع السجلات:

-  **Information**: الطلبات العادية
-  **Warning**: أنشطة مشبوهة
-  **Error**: محاولات هجمات مؤكدة

### أمثلة على السجلات:

```log
[Warning] Rate limit exceeded for client: 192.168.1.100
[Error] SQL injection attempt detected from IP: 10.0.0.50
[Warning] Suspicious User-Agent detected: sqlmap/1.0
[Error] CSRF attack attempt detected from IP: 172.16.0.25
```

## الأداء والتحسين

### تأثير على الأداء:

-  **منخفض جداً**: < 5ms إضافية لكل طلب
-  **ذاكرة فعالة**: تنظيف دوري تلقائي
-  **Thread-safe**: آمن للاستخدام المتزامن

### نصائح التحسين:

1. **قوائم البيضاء**: أضف IPs موثوقة
2. **استثناءات المسارات**: استثني مسارات غير حساسة
3. **تكوين الحدود**: اضبط الحدود حسب الحاجة
4. **مراقبة السجلات**: راقب الأداء بانتظام

## الأمان المتقدم

### حماية إضافية موصى بها:

1. **WAF**: Web Application Firewall
2. **CDN**: Content Delivery Network مع حماية DDoS
3. **SSL/TLS**: شهادات أمان قوية
4. **Database**: تشفير قاعدة البيانات
5. **Secrets**: إدارة آمنة للمفاتيح

### مراقبة مستمرة:

-  مراجعة السجلات يومياً
-  تحديث قوائم الأنماط الضارة
-  اختبار اختراق دوري
-  تحديث التبعيات بانتظام

## استكشاف الأخطاء

### مشاكل شائعة:

**1. معدل مرفوض عالي:**

```bash
# تحقق من الحدود
"MaxRequestsPerMinute": 100  # زيادة الحد
```

**2. حظر IPs مشروعة:**

```bash
# إضافة للقائمة البيضاء
"WhitelistedIps": ["192.168.1.0/24"]
```

**3. مشاكل CSRF:**

```bash
# تحقق من Headers
X-CSRF-Token: <token>
Cookie: __csrf_token=<hashed_token>
```

## اختبار الحماية

### اختبار Rate Limiting:

```bash
# إرسال طلبات متتالية
for i in {1..100}; do curl http://localhost:5000/api/test; done
```

### اختبار SQL Injection:

```bash
# طلب مع SQL injection
curl "http://localhost:5000/api/test?id=1' OR '1'='1"
```

### اختبار XSS:

```bash
# طلب مع XSS payload
curl -d "input=<script>alert('xss')</script>" http://localhost:5000/api/test
```

## الدعم والصيانة

-  📧 **للأسئلة**: راجع الوثائق أو اتصل بالفريق التقني
-  🔄 **التحديثات**: تحقق من التحديثات الأمنية شهرياً
-  📊 **التقارير**: راجع تقارير الأمان أسبوعياً
-  🚨 **الطوارئ**: خطة استجابة للحوادث الأمنية

---

> ⚠️ **تذكير هام**: الأمان عملية مستمرة وليس حدث لمرة واحدة. تأكد من المراجعة والتحديث المستمر لإعدادات الحماية.
