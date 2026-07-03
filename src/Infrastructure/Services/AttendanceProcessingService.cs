using Application.Attendance.Shared;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Domain.Enums;
using Infrastructure.Database;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.Services;

internal sealed class AttendanceProcessingService(
    ApplicationDbContext context,
    ILogger<AttendanceProcessingService> logger,
    // IEmployeeExemptionService exemptionService,
    IAttendanceCalculationService calculationService,
    IDateTimeProvider dateTimeProvider)
    : IAttendanceProcessingService
{

    public async Task CreateAttendanceRecordsAsyncIfNotExists()
    {
        DateTime today = dateTimeProvider.GetUtcNow().Date;
        DateTime todayStart = today.Date;
        DateTime todayEnd = today.Date.AddDays(1).AddTicks(-1);

        try
        {
            var todayDateOnly = DateOnly.FromDateTime(today);

            // Step 1: Load all employees in one query
            List<Employee> employees = await context.Employees
                .Where(e => e.OrganizationalUnitId != null)
                .ToListAsync();

            if (employees.Count == 0)
            {
                //logger.LogInformation("No employees found to create attendance records for {Date}", today);
                return;
            }

            var employeeIds = employees.Select(e => e.Id).ToList();

            // Step 2: Load all existing attendance records for today in one query
            List<Guid> existingAttendanceEmployeeIdsList = await context.Attendances
                .Where(a => a.Date >= todayStart &&
                           a.Date < todayEnd &&
                           employeeIds.Contains(a.EmployeeId))
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToListAsync();

            var existingAttendanceEmployeeIds = existingAttendanceEmployeeIdsList.ToHashSet();

            // Step 3: Load all active schedules for employees in one query
            List<AttendanceSchedule> allSchedules = await context.AttendanceSchedules
                .Where(s => employeeIds.Contains(s.EmployeeId) &&
                            s.IsActive &&
                            s.StartDate <= todayDateOnly &&
                            (!s.EndDate.HasValue || s.EndDate >= todayDateOnly))
                .AsNoTracking()
                .ToListAsync();

            // Step 4: Load all ScheduleDays for today with their Shifts
            var scheduleIds = allSchedules.Select(s => s.Id).ToList();

            List<ScheduleDay> todaysScheduleDays = await context.ScheduleDays
                .Include(sd => sd.Shift)
                .Where(sd => scheduleIds.Contains(sd.AttendanceScheduleId) &&
                            sd.ScheduleDayDate == todayDateOnly &&
                            sd.IsActive)
                .AsNoTracking()
                .ToListAsync();

            // Step 5: Load all Exceptions for today with their Shifts
            List<ScheduleIssue> todaysExceptions = await context.ScheduleIssues
                .Include(e => e.Shift)
                .Where(e => scheduleIds.Contains(e.AttendanceScheduleId) &&
                           e.Date == todayDateOnly)
                .AsNoTracking()
                .ToListAsync();

            // Step 6: Create dictionaries for fast lookup
            var scheduleDaysByScheduleId = todaysScheduleDays
                .GroupBy(sd => sd.AttendanceScheduleId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var exceptionsByScheduleId = todaysExceptions
                .GroupBy(e => e.AttendanceScheduleId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var schedulesByEmployee = allSchedules
                .GroupBy(s => s.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Step 7: Create attendance records in memory
            int recordsCreated = 0;

            foreach (Employee employee in employees)
            {
                // Skip if attendance record already exists
                if (existingAttendanceEmployeeIds.Contains(employee.Id))
                {
                    continue;
                }

                // Get schedules for this employee
                Attendance? newAttendance;

                if (!schedulesByEmployee.TryGetValue(employee.Id, out List<AttendanceSchedule>? candidateSchedules) ||
                    candidateSchedules.Count == 0)
                {
                    // No schedule found, create attendance without shift/schedule
                    newAttendance = new Attendance
                    {
                        EmployeeId = employee.Id,
                        OrganizationId = employee.OrganizationalUnitId!.Value,
                        Date = today,
                        ShiftId = null,
                        AttendanceScheduleId = null,
                        Status = AttendanceStatus.Pending
                    };
                }
                else
                {
                    // Find the first schedule that doesn't exclude today
                    AttendanceSchedule? attendanceSchedule = candidateSchedules
                        .FirstOrDefault(s => !s.ExcludedDates.Contains(todayDateOnly));

                    Guid? shiftId = null;
                    Guid? attendanceScheduleId = null;

                    if (attendanceSchedule is not null)
                    {
                        attendanceScheduleId = attendanceSchedule.Id;

                        // Check for exception first (highest priority)
                        if (exceptionsByScheduleId.TryGetValue(attendanceSchedule.Id, out List<ScheduleIssue>? exceptions) &&
                            exceptions.Count > 0)
                        {
                            ScheduleIssue exception = exceptions[0];
                            shiftId = exception.ShiftId;

                            //logger.LogDebug("Using exception shift {ShiftId} for employee {EmployeeId} on {Date}",
                            //    shiftId, employee.Id, today);
                        }
                        // Otherwise, check for schedule day
                        else if (scheduleDaysByScheduleId.TryGetValue(attendanceSchedule.Id, out List<ScheduleDay>? scheduleDays) &&
                                scheduleDays.Count > 0)
                        {
                            ScheduleDay scheduleDay = scheduleDays[0];
                            shiftId = scheduleDay.ShiftId;

                            //logger.LogDebug("Using schedule day shift {ShiftId} for employee {EmployeeId} on {Date}",
                            //    shiftId, employee.Id, today);
                        }
                        else
                        {
                            logger.LogWarning("No shift found for employee {EmployeeId} on {Date} despite having active schedule {ScheduleId}",
                                employee.Id, today, attendanceSchedule.Id);
                        }
                    }

                    newAttendance = new Attendance
                    {
                        EmployeeId = employee.Id,
                        OrganizationId = employee.OrganizationalUnitId!.Value,
                        Date = today,
                        ShiftId = shiftId,
                        AttendanceScheduleId = attendanceScheduleId,
                        Status = AttendanceStatus.Pending
                    };
                }

                context.Attendances.Add(newAttendance);
                recordsCreated++;
            }

            // Step 6: Save all changes in one batch operation
            if (recordsCreated > 0)
            {
                await context.SaveChangesAsync();
            }

            //logger.LogInformation("Attendance records created successfully for all employees on {Date}. Created {Count} records.", today, recordsCreated);
        }
        catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message?.Contains("duplicate") == true ||
                                              dbEx.InnerException?.Message?.Contains("unique") == true)
        {
            logger.LogWarning(dbEx, "Duplicate attendance record detected for {Date}. This may occur in concurrent scenarios.", today);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating attendance records for all employees on {Date}. See inner exception for details.", today);
        }
    }


    public async Task UpdateAttendancesCheckInAndCheckOutAsync()
    {

        DateTime today = dateTimeProvider.GetUtcNow().Date;
        DateTime processingStart = today.Date.AddDays(-1);
        DateTime processingEnd = today.Date.AddDays(1);

        try
        {
            //logger.LogInformation("Starting to update attendance check in and check out for {Date}", today);

            // Step 1: Load recent attendance logs so overnight check-outs can be paired with the previous work date.
            List<AttendanceLog> attendanceLogs = await context.AttendanceLogs
                .Where(a => a.DateTimeAttend >= processingStart &&
                           a.DateTimeAttend < processingEnd &&
                           !a.IsDeleted)
                .OrderBy(a => a.DateTimeAttend)
                .AsNoTracking()
                .ToListAsync();

            if (attendanceLogs.Count == 0)
            {
                //logger.LogInformation("No attendance logs found for {Date}", today);
                return;
            }

            // Step 2: Get all unique EmpIDs and load employees in one query
            var empIds = attendanceLogs
                .Select(log => log.EmpID)
                .Where(empId => !string.IsNullOrWhiteSpace(empId))
                .Distinct()
                .ToList();

            Dictionary<string, Employee> employeesByEmpId = await context.Employees
                .Where(e => empIds.Contains(e.EmpID) && e.OrganizationalUnitId != null)
                .ToDictionaryAsync(e => e.EmpID, e => e);

            // Step 3: Load attendance records for the same two-day window with Shift navigation property
            List<Attendance> attendanceRecords = await context.Attendances
                .Include(a => a.Shift)
                .Where(a => a.Date >= processingStart && a.Date < processingEnd)
                .ToListAsync();

            var attendanceByEmployeeAndDate = attendanceRecords
                .GroupBy(a => (a.EmployeeId, Date: DateOnly.FromDateTime(a.Date)))
                .ToDictionary(g => g.Key, g => g.First());

            // Step 4: Group logs by employee and resolved work date.
            var resolvedLogs = attendanceLogs
                .Where(log => !string.IsNullOrWhiteSpace(log.EmpID))
                .SelectMany(log =>
                {
                    if (!employeesByEmpId.TryGetValue(log.EmpID, out Employee? employee))
                    {
                        return [];
                    }

                    DateOnly attendanceDate = ResolveAttendanceDate(log, employee.Id, attendanceByEmployeeAndDate);
                    return new[] { new ResolvedAttendanceLog(employee.Id, attendanceDate, log) };
                })
                .ToList();

            var logsByEmployeeAndDate = resolvedLogs
                .GroupBy(item => (item.EmployeeId, item.AttendanceDate))
                .Select(g => new
                {
                    g.Key.EmployeeId,
                    g.Key.AttendanceDate,
                    FirstCheckIn = g.Select(item => item.Log)
                        .Where(l => l.Direct == 1)
                        .OrderBy(l => l.DateTimeAttend)
                        .FirstOrDefault(),
                    LastCheckOut = g.Select(item => item.Log)
                        .Where(l => l.Direct == 2)
                        .OrderByDescending(l => l.DateTimeAttend)
                        .FirstOrDefault()
                })
                .ToList();

            if (logsByEmployeeAndDate.Count == 0)
            {
                //logger.LogInformation("No valid employee IDs found in attendance logs for {Date}", today);
                return;
            }

            // Step 5: Update attendance records
            int recordsUpdated = 0;
            int recordsSkipped = 0;

            foreach (var logGroup in logsByEmployeeAndDate)
            {
                // Get attendance record (should already exist from CreateAttendanceRecordsAsyncIfNotExists)
                if (!attendanceByEmployeeAndDate.TryGetValue((logGroup.EmployeeId, logGroup.AttendanceDate), out Attendance? attendance))
                {
                    logger.LogWarning("Attendance record not found for employee {EmployeeId} on {Date}. Skipping.", logGroup.EmployeeId, logGroup.AttendanceDate);
                    recordsSkipped++;
                    continue;
                }

                // Track if any changes were made
                bool hasChanges = false;

                // Update check-in time (first check-in)
                if (logGroup.FirstCheckIn is not null &&
                    attendance.CheckInTime != logGroup.FirstCheckIn.DateTimeAttend)
                {
                    attendance.CheckInTime = logGroup.FirstCheckIn.DateTimeAttend;
                    attendance.CheckInMethod = LogMethod.Biometric;
                    hasChanges = true;
                }

                // Update check-out time (last check-out)
                if (logGroup.LastCheckOut is not null &&
                    attendance.CheckOutTime != logGroup.LastCheckOut.DateTimeAttend)
                {
                    attendance.CheckOutTime = logGroup.LastCheckOut.DateTimeAttend;
                    attendance.CheckOutMethod = LogMethod.Biometric;
                    hasChanges = true;
                }

                // Calculate metrics and update status based on available data
                if (attendance.Shift is not null)
                {
                    if (logGroup.FirstCheckIn is not null && logGroup.LastCheckOut is not null)
                    {
                        // Both check-in and check-out available - full calculation
                        AttendanceMetrics metrics = calculationService.CalculateMetrics(
                            logGroup.FirstCheckIn.DateTimeAttend,
                            logGroup.LastCheckOut.DateTimeAttend,
                            attendance.Shift);

                        attendance.WorkingMinutes = metrics.WorkingMinutes;
                        attendance.LateMinutes = metrics.LateMinutes;
                        attendance.EarlyLeaveMinutes = metrics.EarlyLeaveMinutes;
                        attendance.OvertimeMinutes = metrics.OvertimeMinutes;
                        attendance.Status = DetermineStatusFromMetrics(metrics);
                        hasChanges = true;
                    }
                    else if (logGroup.FirstCheckIn is not null)
                    {
                        // Only check-in available - calculate late minutes with estimated check-out
                        DateTime tempCheckOut = logGroup.FirstCheckIn.DateTimeAttend.AddHours(8);
                        AttendanceMetrics metrics = calculationService.CalculateMetrics(
                            logGroup.FirstCheckIn.DateTimeAttend,
                            tempCheckOut,
                            attendance.Shift);

                        attendance.LateMinutes = metrics.LateMinutes;
                        attendance.WorkingMinutes = null; // Cannot calculate without actual check-out
                        attendance.EarlyLeaveMinutes = null; // Cannot calculate without actual check-out
                        attendance.OvertimeMinutes = null; // Cannot calculate without actual check-out
                        attendance.Status = metrics.LateMinutes > 0 ? AttendanceStatus.Late : AttendanceStatus.Present;
                        hasChanges = true;
                    }
                    else if (logGroup.LastCheckOut is not null)
                    {
                        // Only check-out available - cannot calculate late minutes, but can calculate early leave
                        // Estimate check-in as 8 hours before check-out
                        DateTime tempCheckIn = logGroup.LastCheckOut.DateTimeAttend.AddHours(-8);
                        AttendanceMetrics metrics = calculationService.CalculateMetrics(
                            tempCheckIn,
                            logGroup.LastCheckOut.DateTimeAttend,
                            attendance.Shift);

                        attendance.LateMinutes = null; // Cannot calculate without actual check-in
                        attendance.WorkingMinutes = null; // Cannot calculate without actual check-in
                        attendance.EarlyLeaveMinutes = metrics.EarlyLeaveMinutes;
                        attendance.OvertimeMinutes = null; // Cannot calculate without actual check-in
                        attendance.Status = metrics.EarlyLeaveMinutes > 0 ? AttendanceStatus.Early_Out : AttendanceStatus.Present;
                        hasChanges = true;
                    }
                }
                else
                {
                    // No shift assigned
                    if (logGroup.FirstCheckIn is not null || logGroup.LastCheckOut is not null)
                    {
                        // If we have at least one time entry, set status to Present
                        attendance.Status = AttendanceStatus.Present;
                        hasChanges = true;
                    }
                    else
                    {
                        // No times and no shift - keep as Pending
                        attendance.Status = AttendanceStatus.Pending;
                    }
                }

                // Update LastUpdatedAt if there were any changes
                if (hasChanges)
                {
                    attendance.LastUpdatedAt = dateTimeProvider.GetUtcNow();
                    recordsUpdated++;
                }
                else
                {
                    recordsSkipped++;
                }
            }

            // Step 7: Save all changes in one batch operation
            if (recordsUpdated > 0)
            {
                await context.SaveChangesAsync();
            }

            //logger.LogInformation(
            //    "Successfully updated attendance check in and check out for {Date}. Updated {Updated} records, Skipped {Skipped} records.",
            //    today, recordsUpdated, recordsSkipped);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating attendance check in and check out for all employees on {Date}. See inner exception for details.", today);
        }

    }

    public Task UpdateAttendancesStatusAsync(Attendance attendance)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAttendanceIfLeaveOrExceptionExistsAsync(Attendance attendance)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAttendanceMetricsAsync(Attendance attendance)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAttendanceNotesAsync(Attendance attendance)
    {
        throw new NotImplementedException();
    }

    public Task<int> CalculateLateMinutesAsync(Attendance attendance)
    {
        throw new NotImplementedException();
    }

    public Task<decimal> CalculateOvertimeHoursAsync(Attendance attendance)
    {


        if (attendance == null || !attendance.CheckInTime.HasValue || !attendance.CheckOutTime.HasValue || attendance.Shift == null)
        {
            return Task.FromResult(0m);
        }

        AttendanceMetrics metrics = calculationService.CalculateMetrics(
            attendance.CheckInTime.Value,
            attendance.CheckOutTime.Value,
            attendance.Shift);

        return Task.FromResult((decimal)(metrics.OvertimeMinutes / 60.0));
    }

    public async Task UpdateAttendanceStatusAsync(Attendance attendance)
    {
        if (attendance is null)
        {
            return;
        }

        // Ensure Shift is loaded if ShiftId exists
        if (attendance.Shift is null && attendance.ShiftId.HasValue)
        {
            try
            {
                Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Attendance> entry = context.Entry(attendance);

                // Only load if entity is tracked
                if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                {
                    await entry
                        .Reference(a => a.Shift)
                        .LoadAsync();
                }
                else
                {
                    // If entity is not tracked, reload it with Shift
                    Attendance? reloadedAttendance = await context.Attendances
                        .Include(a => a.Shift)
                        .FirstOrDefaultAsync(a => a.Id == attendance.Id);

                    if (reloadedAttendance is not null)
                    {
                        // Copy Shift reference to the original attendance object
                        attendance.Shift = reloadedAttendance.Shift;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load Shift for attendance {AttendanceId}. Status will be set to Pending.", attendance.Id);
                attendance.Status = AttendanceStatus.Pending;
                return;
            }
        }

        if (attendance.Shift is not null && attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
        {
            AttendanceMetrics metrics = calculationService.CalculateMetrics(
                attendance.CheckInTime.Value,
                attendance.CheckOutTime.Value,
                attendance.Shift);

            attendance.WorkingMinutes = metrics.WorkingMinutes;
            attendance.LateMinutes = metrics.LateMinutes;
            attendance.EarlyLeaveMinutes = metrics.EarlyLeaveMinutes;
            attendance.OvertimeMinutes = metrics.OvertimeMinutes;

            attendance.Status = DetermineStatusFromMetrics(metrics);
        }
        else if (attendance.Shift is not null && attendance.CheckInTime.HasValue)
        {
            // When only check-in is available, calculate metrics with estimated check-out
            DateTime tempCheckOut = attendance.CheckInTime.Value.AddHours(8);
            AttendanceMetrics metrics = calculationService.CalculateMetrics(
                attendance.CheckInTime.Value,
                tempCheckOut,
                attendance.Shift);

            // Update all metrics for consistency
            attendance.LateMinutes = metrics.LateMinutes;
            attendance.WorkingMinutes = null; // Cannot calculate without actual check-out
            attendance.EarlyLeaveMinutes = null; // Cannot calculate without actual check-out
            attendance.OvertimeMinutes = null; // Cannot calculate without actual check-out

            attendance.Status = metrics.LateMinutes > 0 ? AttendanceStatus.Late : AttendanceStatus.Present;
        }
        else
        {
            attendance.Status = AttendanceStatus.Pending;
        }

        // Note: SaveChangesAsync is called by the caller (UpdateAttendancesCheckInAndCheckOutAsync)
        // to batch all updates together for better performance
    }

    private DateOnly ResolveAttendanceDate(
        AttendanceLog log,
        Guid employeeId,
        Dictionary<(Guid EmployeeId, DateOnly Date), Attendance> attendanceByEmployeeAndDate)
    {
        DateOnly logWorkDate = log.DateWork;

        if (log.Direct != 2)
        {
            return logWorkDate;
        }

        DateOnly previousWorkDate = logWorkDate.AddDays(-1);

        if (!attendanceByEmployeeAndDate.TryGetValue((employeeId, previousWorkDate), out Attendance? previousAttendance) ||
            !IsOvernightShift(previousAttendance.Shift))
        {
            return logWorkDate;
        }

        DateTime localAttendTime = dateTimeProvider.ConvertToLocalTime(log.DateTimeAttend);
        var localTime = TimeOnly.FromDateTime(localAttendTime);

        if (localTime < previousAttendance.Shift!.StartTime &&
            localTime < TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)))
        {
            return previousWorkDate;
        }

        return logWorkDate;
    }

    private static bool IsOvernightShift(Shift? shift)
    {
        return shift is not null && shift.EndTime < shift.StartTime;
    }

    private sealed record ResolvedAttendanceLog(
        Guid EmployeeId,
        DateOnly AttendanceDate,
        AttendanceLog Log);

    private static AttendanceStatus DetermineStatusFromMetrics(AttendanceMetrics metrics)
    {
        // الأولوية: انصراف مبكر > عمل إضافي (أكثر من ساعة) > متأخر > حضور

        if (metrics.EarlyLeaveMinutes > 0)
        {
            return AttendanceStatus.Early_Out;
        }

        // عمل إضافي: أكثر من 60 دقيقة (ساعة واحدة)
        if (metrics.OvertimeMinutes > 60)
        {
            return AttendanceStatus.Overtime;
        }

        if (metrics.LateMinutes > 0)
        {
            return AttendanceStatus.Late;
        }

        return AttendanceStatus.Present;
    }



}
