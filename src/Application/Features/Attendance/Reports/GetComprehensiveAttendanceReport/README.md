# تقرير الحضور الشامل

## الوصف

تقرير شامل لحالة الحضور والانصراف يوفر موقف يومي أو أسبوعي أو شهري لجميع المقاييس المهمة:

-  الحضور والغياب
-  التأخير والانصراف المبكر
-  العمل الإضافي
-  الإجازات الساعية
-  ساعات العمل والراحة
-  تقييم الأداء

## العمليات

-  **Query**: `GetComprehensiveAttendanceReportQuery`
-  **Handler**: `GetComprehensiveAttendanceReportQueryHandler`
-  **Validator**: `GetComprehensiveAttendanceReportQueryValidator`

## القواعد التجارية

-  يجب أن يكون نطاق التاريخ صحيحاً ومعقولاً
-  يمكن تصفية النتائج حسب المنظمة، القسم، الموظف، أو المدير
-  يدعم أنواع تقارير متعددة (يومي، أسبوعي، شهري، مخصص)
-  يتضمن جميع أنواع المقاييس (الحضور، التأخير، الإضافي، الإجازات الساعية)
-  يحسب درجات الأداء تلقائياً
-  يدعم التصدير بصيغ متعددة
-  **تحسينات الأداء**: يستخدم `AsSplitQuery()` لتقسيم الاستعلامات المعقدة
-  **معالجة الأخطاء**: تحقق شامل من وجود البيانات قبل معالجتها
-  **معالجة الحالات الفارغة**: إرجاع تقرير فارغ بدلاً من خطأ عند عدم وجود بيانات
-  **مرونة في النتائج**: يعمل حتى لو لم توجد بيانات حضور أو جداول زمنية

## المعاملات المدخلة

-  `StartDate` (DateTime) - تاريخ بداية التقرير
-  `EndDate` (DateTime) - تاريخ نهاية التقرير
-  `OrganizationId` (Guid, اختياري) - تصفية حسب المنظمة
-  `OrganizationalUnitId` (Guid, اختياري) - تصفية حسب القسم
-  `EmployeeId` (Guid, اختياري) - تصفية حسب موظف محدد
-  `ManagerId` (Guid, اختياري) - تصفية حسب فريق مدير محدد
-  `ReportType` (ReportType enum) - نوع التقرير
-  `GroupBy` (string, اختياري) - خيار التجميع
-  `IncludeHourlyLeaves` (bool) - تضمين الإجازات الساعية
-  `IncludeOvertime` (bool) - تضمين العمل الإضافي
-  `IncludeLateDetails` (bool) - تضمين تفاصيل التأخير
-  `IncludeEarlyDepartures` (bool) - تضمين الانصراف المبكر
-  `ExportFormat` (ExportFormat enum, اختياري) - صيغة التصدير

## المخرجات

-  `ComprehensiveAttendanceReportResponse` مع بيانات التقرير الشاملة
-  إحصائيات شاملة للمنظمة/القسم
-  ملخصات تفصيلية لكل موظف
-  ملخصات يومية
-  تقييمات الأداء
-  ملف تصدير (إذا طُلب)

## الكيانات المرتبطة

-  `Attendance` - المصدر الرئيسي للبيانات
-  `Employee` - معلومات الموظفين
-  `OrganizationalUnit` - معلومات الأقسام
-  `AttendanceBreak` - بيانات الإجازات الساعية والراحة
-  `AttendanceSchedule` - الجداول الزمنية للموظفين
-  `ScheduleDay` - أيام العمل في الجداول
-  `ScheduleIssue` - استثناءات الجداول الزمنية

## أمثلة الاستخدام

### تقرير يومي لقسم محدد

```csharp
var query = new GetComprehensiveAttendanceReportQuery
{
    StartDate = DateTime.Today,
    EndDate = DateTime.Today,
    OrganizationalUnitId = departmentId,
    ReportType = ReportType.Daily,
    IncludeHourlyLeaves = true,
    IncludeOvertime = true
};
```

### تقرير أسبوعي لموظف محدد

```csharp
var query = new GetComprehensiveAttendanceReportQuery
{
    StartDate = DateTime.Today.AddDays(-7),
    EndDate = DateTime.Today,
    EmployeeId = employeeId,
    ReportType = ReportType.Weekly,
    IncludeLateDetails = true,
    IncludeEarlyDepartures = true
};
```

### تقرير شهري للمنظمة كاملة

```csharp
var query = new GetComprehensiveAttendanceReportQuery
{
    StartDate = new DateTime(2024, 1, 1),
    EndDate = new DateTime(2024, 1, 31),
    OrganizationId = organizationId,
    ReportType = ReportType.Monthly,
    ExportFormat = ExportFormat.Excel
};
```

## المقاييس المحسوبة

### إحصائيات الحضور

-  إجمالي الموظفين
-  عدد الحضور
-  عدد الغياب
-  عدد الإجازات
-  معدل الحضور

### إحصائيات التأخير

-  عدد التأخيرات
-  إجمالي دقائق التأخير
-  متوسط دقائق التأخير
-  معدل التأخير

### إحصائيات الانصراف المبكر

-  عدد الانصراف المبكر
-  إجمالي دقائق الانصراف المبكر
-  متوسط دقائق الانصراف المبكر
-  معدل الانصراف المبكر

### إحصائيات العمل الإضافي

-  عدد أيام العمل الإضافي
-  إجمالي ساعات العمل الإضافي
-  متوسط ساعات العمل الإضافي
-  معدل العمل الإضافي

### إحصائيات الإجازات الساعية

-  عدد أيام الإجازات الساعية
-  إجمالي ساعات الإجازات الساعية
-  متوسط ساعات الإجازات الساعية
-  عدد طلبات الإجازات الساعية

### تقييم الأداء

-  درجة الأداء (0-100)
-  مستوى الأداء (ممتاز، جيد، مقبول، ضعيف)
-  حساب تلقائي بناءً على الحضور، الدقة، إكمال الدوام، والعمل الإضافي
