# Reports Endpoints - إندبوينت التقارير

## نظرة عامة

هذا المجلد يحتوي على جميع إندبوينت التقارير في النظام، والتي توفر وصول REST API للتقارير المختلفة.

## الإندبوينت المتاحة

### 1. GetAttendanceReport

-  **المسار**: `GET /reports/attendance-summary`
-  **الوصف**: تقرير شامل لإحصائيات الحضور والانصراف
-  **الملف**: `GetAttendanceReport.cs`

### 2. GetOrganizationalSummary

-  **المسار**: `GET /reports/organizational-summary`
-  **الوصف**: ملخص مختصر للوحدات التنظيمية
-  **الملف**: `GetOrganizationalSummary.cs`

## الميزات المشتركة

### الأمان

-  جميع الإندبوينت تتطلب مصادقة (Authentication)
-  جميع الإندبوينت تتطلب تفويض (Authorization)

### التوثيق

-  جميع الإندبوينت مدعومة في Swagger UI
-  جميع الإندبوينت تدعم OpenAPI 3.0
-  جميع الإندبوينت تحتوي على وصف وملخص باللغة العربية

### معالجة الأخطاء

-  استخدام `CustomResults.Problem` لمعالجة الأخطاء
-  إرجاع رسائل خطأ مناسبة باللغة العربية
-  دعم `CancellationToken` للعمليات الطويلة

## هيكل الإندبوينت

كل إندبوينت يتبع النمط التالي:

```csharp
internal sealed class EndpointName : IEndpoint
{
    public sealed class Request
    {
        // معاملات الطلب
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("path", async (
            [AsParameters] Request request,
            IQueryHandler<QueryType, ApiResponse<ResponseType>> handler,
            CancellationToken cancellationToken) =>
        {
            // منطق الإندبوينت
        })
        .WithTags(Tags.Reports)
        .RequireAuthorization()
        .WithName("EndpointName")
        .WithSummary("الملخص")
        .WithDescription("الوصف")
        .WithOpenApi();
    }
}
```

## التسجيل التلقائي

جميع الإندبوينت يتم تسجيلها تلقائياً من خلال `EndpointExtensions.cs` في مجلد `Extensions`.

## التطوير المستقبلي

-  إضافة إندبوينت لتصدير التقارير (PDF, Excel)
-  إضافة إندبوينت للتقارير المخصصة
-  إضافة إندبوينت للتقارير المجدولة
-  إضافة إندبوينت للتقارير التفاعلية

