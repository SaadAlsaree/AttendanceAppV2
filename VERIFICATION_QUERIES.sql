-- ============================================================
-- SQL Queries للتحقق من إصلاح مشكلة ScheduleDays
-- ============================================================

-- 1. التحقق من عدد الأيام لكل schedule (يجب أن يكون 7)
-- إذا رجع أي صف، هناك schedule بعدد أيام خاطئ!
SELECT 
    AttendanceScheduleId,
    COUNT(*) as ActualDaysCount,
    COUNT(DISTINCT DayOfWeek) as UniqueDaysCount,
    7 - COUNT(*) as MissingDays
FROM "ScheduleDays"
GROUP BY AttendanceScheduleId
HAVING COUNT(*) != 7
ORDER BY ActualDaysCount;

-- النتيجة المتوقعة بعد الإصلاح: 0 rows


-- 2. عرض الأيام المحفوظة لكل schedule (يجب أن تكون 7 أيام دائماً)
SELECT 
    sd.AttendanceScheduleId,
    STRING_AGG(sd.DayOfWeek, ', ' ORDER BY 
        CASE sd.DayOfWeek
            WHEN 'Sunday' THEN 1
            WHEN 'Monday' THEN 2
            WHEN 'Tuesday' THEN 3
            WHEN 'Wednesday' THEN 4
            WHEN 'Thursday' THEN 5
            WHEN 'Friday' THEN 6
            WHEN 'Saturday' THEN 7
        END
    ) as SavedDays,
    COUNT(*) as DaysCount
FROM "ScheduleDays" sd
GROUP BY sd.AttendanceScheduleId
ORDER BY DaysCount;

-- النتيجة المتوقعة: 
-- كل schedule يجب أن يحتوي على: "Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday"
-- DaysCount = 7 لكل schedule


-- 3. التحقق من عدم وجود Guid.Empty في AttendanceScheduleId
SELECT 
    Id,
    AttendanceScheduleId,
    DayOfWeek
FROM "ScheduleDays"
WHERE AttendanceScheduleId = '00000000-0000-0000-0000-000000000000'
ORDER BY CreatedAt DESC;

-- النتيجة المتوقعة بعد الإصلاح: 0 rows


-- 4. التحقق من عدم وجود تكرار في (AttendanceScheduleId, DayOfWeek)
-- بسبب الـ Unique Index، يجب ألا يكون هناك تكرار
SELECT 
    AttendanceScheduleId,
    DayOfWeek,
    COUNT(*) as DuplicateCount
FROM "ScheduleDays"
GROUP BY AttendanceScheduleId, DayOfWeek
HAVING COUNT(*) > 1;

-- النتيجة المتوقعة: 0 rows


-- 5. عرض أحدث schedules مع عدد أيامها
SELECT 
    asch.Id as ScheduleId,
    asch.EmployeeId,
    asch.StartDate,
    asch.EndDate,
    COUNT(sd.Id) as DaysCount,
    STRING_AGG(sd.DayOfWeek, ', ' ORDER BY 
        CASE sd.DayOfWeek
            WHEN 'Sunday' THEN 1
            WHEN 'Monday' THEN 2
            WHEN 'Tuesday' THEN 3
            WHEN 'Wednesday' THEN 4
            WHEN 'Thursday' THEN 5
            WHEN 'Friday' THEN 6
            WHEN 'Saturday' THEN 7
        END
    ) as Days
FROM "AttendanceSchedules" asch
LEFT JOIN "ScheduleDays" sd ON asch.Id = sd.AttendanceScheduleId
WHERE asch.CreatedAt >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY asch.Id, asch.EmployeeId, asch.StartDate, asch.EndDate
ORDER BY asch.CreatedAt DESC
LIMIT 20;

-- النتيجة المتوقعة:
-- DaysCount = 7 لكل schedule
-- Days = "Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday"


-- 6. إحصائيات عامة
SELECT 
    'Total Schedules' as Metric,
    COUNT(*) as Value
FROM "AttendanceSchedules"
UNION ALL
SELECT 
    'Total ScheduleDays' as Metric,
    COUNT(*) as Value
FROM "ScheduleDays"
UNION ALL
SELECT 
    'Expected ScheduleDays' as Metric,
    COUNT(*) * 7 as Value
FROM "AttendanceSchedules"
UNION ALL
SELECT 
    'Missing ScheduleDays' as Metric,
    (COUNT(*) * 7) - (SELECT COUNT(*) FROM "ScheduleDays") as Value
FROM "AttendanceSchedules";

-- النتيجة المتوقعة:
-- Missing ScheduleDays = 0


-- 7. التحقق من الأيام الناقصة لكل schedule (إن وجدت)
WITH AllDays AS (
    SELECT UNNEST(ARRAY['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']) as DayName
),
ScheduleIds AS (
    SELECT DISTINCT AttendanceScheduleId FROM "ScheduleDays"
)
SELECT 
    s.AttendanceScheduleId,
    d.DayName as MissingDay
FROM ScheduleIds s
CROSS JOIN AllDays d
WHERE NOT EXISTS (
    SELECT 1 
    FROM "ScheduleDays" sd 
    WHERE sd.AttendanceScheduleId = s.AttendanceScheduleId 
    AND sd.DayOfWeek = d.DayName
)
ORDER BY s.AttendanceScheduleId, 
    CASE d.DayName
        WHEN 'Sunday' THEN 1
        WHEN 'Monday' THEN 2
        WHEN 'Tuesday' THEN 3
        WHEN 'Wednesday' THEN 4
        WHEN 'Thursday' THEN 5
        WHEN 'Friday' THEN 6
        WHEN 'Saturday' THEN 7
    END;

-- النتيجة المتوقعة بعد الإصلاح: 0 rows


-- 8. مقارنة البيانات قبل وبعد الإصلاح
SELECT 
    DATE(CreatedAt) as Date,
    COUNT(DISTINCT AttendanceScheduleId) as SchedulesCreated,
    COUNT(*) as TotalDays,
    COUNT(*) / NULLIF(COUNT(DISTINCT AttendanceScheduleId), 0) as AvgDaysPerSchedule
FROM "ScheduleDays"
GROUP BY DATE(CreatedAt)
ORDER BY Date DESC;

-- النتيجة المتوقعة بعد الإصلاح:
-- AvgDaysPerSchedule = 7.0 للتواريخ الجديدة


-- ============================================================
-- Query لحذف البيانات القديمة الخاطئة (إذا لزم الأمر)
-- ⚠️ تحذير: استخدم بحذر! سيحذف جميع schedules التي تم إنشاؤها اليوم
-- ============================================================

-- UNCOMMENT ONLY IF YOU WANT TO DELETE TODAY'S SCHEDULES AND RE-CREATE THEM
/*
BEGIN;

-- حذف ScheduleDays للـ schedules المُنشأة اليوم
DELETE FROM "ScheduleDays"
WHERE AttendanceScheduleId IN (
    SELECT Id FROM "AttendanceSchedules" 
    WHERE DATE(CreatedAt) = CURRENT_DATE
);

-- حذف AttendanceSchedules المُنشأة اليوم
DELETE FROM "AttendanceSchedules"
WHERE DATE(CreatedAt) = CURRENT_DATE;

-- التحقق من العدد المحذوف
SELECT 
    'Deleted ScheduleDays' as Item,
    (SELECT COUNT(*) FROM "ScheduleDays" WHERE DATE(CreatedAt) = CURRENT_DATE) as Count
UNION ALL
SELECT 
    'Deleted Schedules' as Item,
    (SELECT COUNT(*) FROM "AttendanceSchedules" WHERE DATE(CreatedAt) = CURRENT_DATE) as Count;

-- إذا كنت متأكد، قم بعمل COMMIT
-- COMMIT;

-- وإلا، قم بعمل ROLLBACK
ROLLBACK;
*/


-- ============================================================
-- Query للتحقق من صحة الـ Foreign Keys
-- ============================================================

-- التحقق من وجود ScheduleDays ليس لها AttendanceSchedule
SELECT 
    sd.Id,
    sd.AttendanceScheduleId,
    sd.DayOfWeek
FROM "ScheduleDays" sd
LEFT JOIN "AttendanceSchedules" asch ON sd.AttendanceScheduleId = asch.Id
WHERE asch.Id IS NULL;

-- النتيجة المتوقعة: 0 rows


-- ============================================================
-- Performance Check: عدد الـ Schedules والأيام حسب الموظف
-- ============================================================

SELECT 
    e.Id as EmployeeId,
    e.FirstName || ' ' || e.LastName as EmployeeName,
    COUNT(DISTINCT asch.Id) as SchedulesCount,
    COUNT(sd.Id) as TotalDays,
    COUNT(sd.Id) / NULLIF(COUNT(DISTINCT asch.Id), 0) as AvgDaysPerSchedule
FROM "Employees" e
LEFT JOIN "AttendanceSchedules" asch ON e.Id = asch.EmployeeId AND asch.IsActive = true
LEFT JOIN "ScheduleDays" sd ON asch.Id = sd.AttendanceScheduleId
GROUP BY e.Id, e.FirstName, e.LastName
HAVING COUNT(DISTINCT asch.Id) > 0
ORDER BY AvgDaysPerSchedule;

-- النتيجة المتوقعة:
-- AvgDaysPerSchedule = 7.0 لكل موظف (إذا كان لديه schedule واحد فقط)
