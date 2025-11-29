# Security Middleware

هذا المجلد يحتوي على middleware للحماية من الهجمات المختلفة والتأمين التطبيق.

## المكونات الأمنية

### 1. Rate Limiting Middleware (`RateLimitingMiddleware.cs`)

-  **الغرض**: منع هجمات DDoS وتحديد عدد الطلبات
-  **الميزات**:
   -  تحديد 60 طلب في الدقيقة الواحدة
   -  تحديد 1000 طلب في الساعة الواحدة
   -  حظر مؤقت لمدة 15 دقيقة عند تجاوز الحد
   -  تنظيف تلقائي للذاكرة

### 2. IP Blocking Middleware (`IpBlockingMiddleware.cs`)

-  **الغرض**: حظر عناوين IP المشبوهة
-  **الميزات**:
   -  كشف الأنشطة المشبوهة
   -  حظر IPs بعد 10 محاولات مشبوهة
   -  قائمة بيضاء للـ IPs الموثوقة
   -  حظر مؤقت لمدة 24 ساعة

### 3. Request Validation Middleware (`RequestValidationMiddleware.cs`)

-  **الغرض**: فحص الطلبات للكشف عن المحتوى المشبوه
-  **الميزات**:
   -  فحص SQL injection patterns
   -  فحص XSS attacks
   -  فحص Path traversal attacks
   -  فحص Command injection
   -  فحص File uploads الخطيرة
   -  تحديد حجم الطلبات والـ Headers

### 4. SQL Injection Protection Middleware (`SqlInjectionProtectionMiddleware.cs`)

-  **الغرض**: حماية متقدمة من SQL injection
-  **الميزات**:
   -  كشف Union-based injection
   -  كشف Boolean-based blind injection
   -  كشف Time-based blind injection
   -  كشف Error-based injection
   -  كشف Information schema access
   -  فحص JSON body وForm data

### 5. Security Headers Middleware (`SecurityHeadersMiddleware.cs`)

-  **الغرض**: إضافة Headers أمنية
-  **الميزات**:
   -  X-Frame-Options: DENY
   -  X-Content-Type-Options: nosniff
   -  X-XSS-Protection: 1; mode=block
   -  Strict-Transport-Security
   -  Content-Security-Policy
   -  Referrer-Policy
   -  Permissions-Policy
   -  Cross-Origin policies

## الاستخدام

### تفعيل جميع المكونات الأمنية

```csharp
app.UseSecurityMiddleware();
```

### تفعيل مكونات محددة

```csharp
app.UseRateLimiting();
app.UseIpBlocking();
app.UseRequestValidation();
app.UseSqlInjectionProtection();
app.UseSecurityHeaders();
```

## التكوين

يمكن تكوين المكونات الأمنية في `appsettings.json`:

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
         "WhitelistedIps": ["127.0.0.1", "::1"]
      }
   }
}
```

## السجلات

جميع المكونات تسجل الأنشطة المشبوهة ومحاولات الهجمات:

-  **تحذيرات**: للأنشطة المشبوهة
-  **أخطاء**: لمحاولات الهجمات المؤكدة
-  **معلومات**: للأحداث الأمنية العامة

## الأداء

-  استخدام `ConcurrentDictionary` للـ thread safety
-  تنظيف دوري للذاكرة
-  فحص فعال للـ patterns باستخدام compiled regex
-  حد أدنى من تأثير على الأداء

## التخصيص

يمكن تخصيص:

-  معايير الكشف
-  مدة الحظر
-  قوائم البيضاء والسوداء
-  رسائل الخطأ
-  Headers الأمنية

## الملاحظات الأمنية

1. **ترتيب Middleware مهم**: Security headers أولاً، ثم rate limiting، ثم validation
2. **قوائم البيضاء**: تأكد من إضافة IPs الموثوقة
3. **المراقبة**: راقب السجلات للكشف عن الهجمات
4. **التحديث**: احرص على تحديث patterns الكشف بانتظام
5. **اختبار**: اختبر التكوين في بيئة التطوير أولاً

## حالات الاستجابة

-  **403 Forbidden**: IP محظور أو نشاط مشبوه
-  **400 Bad Request**: طلب غير صحيح أو خطير
-  **429 Too Many Requests**: تجاوز حد الطلبات
-  **500 Internal Server Error**: خطأ في المعالجة

## التوافق

-  ASP.NET Core 8+
-  يعمل مع أي نوع من الـ hosting
-  متوافق مع reverse proxies
-  يدعم IPv4 و IPv6
