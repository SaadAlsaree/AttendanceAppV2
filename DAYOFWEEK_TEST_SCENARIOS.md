# Test Scenarios for DayOfWeek Fix

## Scenario 1: Creating a schedule for date range 2025-10-01 to 2025-10-07

### Expected Behavior:

For each employee, 7 ScheduleDay records are created:

1. Sunday (DayOfWeek = 1)
2. Monday (DayOfWeek = 2)
3. Tuesday (DayOfWeek = 3)
4. Wednesday (DayOfWeek = 4)
5. Thursday (DayOfWeek = 5)
6. Friday (DayOfWeek = 6)
7. Saturday (DayOfWeek = 7)

### When retrieving shift for specific dates:

-  2025-10-01 (Wednesday) → Matches ScheduleDay with DayOfWeek=Wednesday(4) ✓
-  2025-10-02 (Thursday) → Matches ScheduleDay with DayOfWeek=Thursday(5) ✓
-  2025-10-03 (Friday) → Matches ScheduleDay with DayOfWeek=Friday(6) ✓
-  2025-10-04 (Saturday) → Matches ScheduleDay with DayOfWeek=Saturday(7) ✓
-  2025-10-05 (Sunday) → Matches ScheduleDay with DayOfWeek=Sunday(1) ✓
-  2025-10-06 (Monday) → Matches ScheduleDay with DayOfWeek=Monday(2) ✓
-  2025-10-07 (Tuesday) → Matches ScheduleDay with DayOfWeek=Tuesday(3) ✓

## Scenario 2: Testing the conversion

### C# System.DayOfWeek to Domain.Enums.DayOfWeek

```csharp
// For date 2025-10-01 (Wednesday)
var systemDay = new DateOnly(2025, 10, 1).DayOfWeek; // Wednesday (value: 3)
var domainDay = systemDay.ToDomain(); // Wednesday (value: 4)

// Conversion formula: Domain = System + 1
// System.DayOfWeek.Wednesday (3) + 1 = Domain.Enums.DayOfWeek.Wednesday (4)
```

### Database Query

```sql
-- Get the shift for employee on 2025-10-01 (Wednesday)
SELECT sd.DayOfWeek, s.Name as ShiftName
FROM ScheduleDays sd
JOIN Shifts s ON sd.ShiftId = s.Id
WHERE sd.AttendanceScheduleId = '<schedule-id>'
  AND sd.DayOfWeek = 'Wednesday'  -- EF converts enum to string
  AND sd.IsActive = true;
```

## Scenario 3: Edge cases

### Leap year

-  February 29, 2024 is Thursday → Should match DayOfWeek.Thursday(5) ✓

### Year boundary

-  December 31, 2024 is Tuesday → Should match DayOfWeek.Tuesday(3) ✓
-  January 1, 2025 is Wednesday → Should match DayOfWeek.Wednesday(4) ✓

### Same day across different weeks

-  2025-10-01 (Wednesday) → DayOfWeek.Wednesday(4)
-  2025-10-08 (Wednesday) → DayOfWeek.Wednesday(4)
-  2025-10-15 (Wednesday) → DayOfWeek.Wednesday(4)
   All should match the SAME ScheduleDay record ✓

## Test Data

### Input Command

```json
{
   "startDate": "2025-10-01",
   "endDate": "2025-12-31",
   "shiftId": "01999b23-acc0-794f-8b43-08b4991b62ad"
}
```

### Expected Database State (per employee)

#### AttendanceSchedules table

| Id     | EmployeeId | StartDate  | EndDate    | ScheduleType | IsActive |
| ------ | ---------- | ---------- | ---------- | ------------ | -------- |
| guid-1 | emp-1      | 2025-10-01 | 2025-12-31 | Regular      | true     |

#### ScheduleDays table (for schedule guid-1)

| Id   | AttendanceScheduleId | DayOfWeek | ShiftId | IsActive |
| ---- | -------------------- | --------- | ------- | -------- |
| sd-1 | guid-1               | Sunday    | shift-1 | true     |
| sd-2 | guid-1               | Monday    | shift-1 | true     |
| sd-3 | guid-1               | Tuesday   | shift-1 | true     |
| sd-4 | guid-1               | Wednesday | shift-1 | true     |
| sd-5 | guid-1               | Thursday  | shift-1 | true     |
| sd-6 | guid-1               | Friday    | shift-1 | true     |
| sd-7 | guid-1               | Saturday  | shift-1 | true     |

### Verification Queries

```sql
-- 1. Count ScheduleDays per schedule (should be 7)
SELECT AttendanceScheduleId, COUNT(*) as DayCount
FROM ScheduleDays
GROUP BY AttendanceScheduleId;

-- 2. Verify order and values
SELECT DayOfWeek,
       CASE DayOfWeek
           WHEN 'Sunday' THEN 1
           WHEN 'Monday' THEN 2
           WHEN 'Tuesday' THEN 3
           WHEN 'Wednesday' THEN 4
           WHEN 'Thursday' THEN 5
           WHEN 'Friday' THEN 6
           WHEN 'Saturday' THEN 7
       END as ExpectedValue
FROM ScheduleDays
WHERE AttendanceScheduleId = '<schedule-id>'
ORDER BY ExpectedValue;

-- 3. Check for duplicates (should return 0 rows)
SELECT AttendanceScheduleId, DayOfWeek, COUNT(*) as Count
FROM ScheduleDays
GROUP BY AttendanceScheduleId, DayOfWeek
HAVING COUNT(*) > 1;
```

## Manual Testing Steps

1. **Setup:**

   -  Ensure you have employees in the database
   -  Ensure you have a valid shift with id `01999b23-acc0-794f-8b43-08b4991b62ad`

2. **Execute bulk insert:**

   ```http
   POST /api/attendance-schedules/bulk-insert
   Content-Type: application/json

   {
     "startDate": "2025-10-01",
     "endDate": "2025-12-31",
     "shiftId": "01999b23-acc0-794f-8b43-08b4991b62ad"
   }
   ```

3. **Verify results:**

   -  Check that each employee has exactly 7 ScheduleDay records
   -  Verify the days are: Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday
   -  Confirm all have the same ShiftId

4. **Test date matching:**
   -  Run attendance report for 2025-10-01
   -  Verify it shows Wednesday shift
   -  Run for 2025-10-05
   -  Verify it shows Sunday shift

## Success Criteria

✓ Build passes without errors
✓ All unit tests pass
✓ Each employee schedule has exactly 7 ScheduleDay records
✓ ScheduleDays are in correct order (Sunday through Saturday)
✓ No duplicate days per schedule
✓ Date-to-day matching works correctly for all dates in the range
✓ System.DayOfWeek → Domain.DayOfWeek conversion is accurate
