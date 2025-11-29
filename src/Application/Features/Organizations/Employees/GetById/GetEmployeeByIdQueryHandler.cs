using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Employees.GetById;

internal sealed class GetEmployeeByIdQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetEmployeeByIdQuery, ApiResponse<GetEmployeeByIdVm>>
{
    public async Task<Result<ApiResponse<GetEmployeeByIdVm>>> Handle(GetEmployeeByIdQuery query, CancellationToken cancellationToken)
    {
        Employee employee = await context.Employees
            .Include(e => e.OrganizationalUnit)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .Include(e => e.AttendanceSchedules)
                .ThenInclude(as_ => as_.Exceptions)
            .Include(e => e.AttendanceSchedules)
                .ThenInclude(as_ => as_.ScheduleDays)
            .Include(e => e.Attendances)
                .ThenInclude(a => a.Shift)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.Id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<ApiResponse<GetEmployeeByIdVm>>(EmployeeErrors.NotFound(query.Id));
        }

        // Get the active attendance schedule
        AttendanceSchedule? activeSchedule = employee.AttendanceSchedules
            .Where(as_ => as_.IsActive)
            .OrderByDescending(as_ => as_.StartDate)
            .FirstOrDefault();

        AttendanceScheduleDto attendanceScheduleDto = activeSchedule is not null ? new AttendanceScheduleDto
        {
            Id = activeSchedule.Id,
            StartDate = activeSchedule.StartDate,
            EndDate = activeSchedule.EndDate,
            ScheduleType = activeSchedule.ScheduleType,
            IsActive = activeSchedule.IsActive,
            Notes = activeSchedule.Notes,
            ExcludedDates = activeSchedule.ExcludedDates,
            Exceptions = activeSchedule.Exceptions.Select(e => new ScheduleIssueDto
            {
                Id = e.Id,
                Date = e.Date,
                ShiftId = e.ShiftId,
                Reason = e.Reason,
                ExceptionType = e.ExceptionType
            }).ToList(),
            ScheduleDays = activeSchedule.ScheduleDays
                .OrderBy(sd => sd.ScheduleDayDate)
                .Select(sd => new ScheduleDayDto
                {
                    AttendanceScheduleId = sd.AttendanceScheduleId,
                    ScheduleDayDate = sd.ScheduleDayDate,
                    ShiftId = sd.ShiftId,
                    IsActive = sd.IsActive,
                    Notes = sd.Notes
                }).ToList()
        } : new AttendanceScheduleDto();

        var attendanceDtos = employee.Attendances.Select(a => new AttendanceDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            OrganizationId = a.OrganizationId,
            Date = a.Date,
            CheckInTime = a.CheckInTime,
            CheckOutTime = a.CheckOutTime,
            Status = a.Status,
            ShiftId = a.ShiftId,
            WorkingMinutes = a.WorkingMinutes,
            BreakMinutes = a.BreakMinutes,
            OvertimeMinutes = a.OvertimeMinutes,
            LateMinutes = a.LateMinutes,
            EarlyLeaveMinutes = a.EarlyLeaveMinutes,
            Notes = a.Notes,
            CheckInMethod = a.CheckInMethod,
            CheckOutMethod = a.CheckOutMethod
        })
        .OrderBy(a => a.Date)
        .Take(15)
        .Where(a => a.CheckInTime.HasValue || a.CheckOutTime.HasValue).ToList();

        // حساب إحصائيات العمل
        // حساب عدد أيام العمل: جميع السجلات التي تحتوي على CheckInTime (الموظف كان حاضراً)
        var attendancesWithCheckIn = employee.Attendances
            .Where(a => a.CheckInTime.HasValue)
            .ToList();

        int totalWorkingDays = attendancesWithCheckIn.Count;

        // حساب ساعات العمل
        // 1. السجلات التي تحتوي على WorkingMinutes (تم تسجيل الدخول والخروج)
        double totalWorkingHours = attendancesWithCheckIn
            .Where(a => a.WorkingMinutes.HasValue)
            .Sum(a => a.WorkingMinutes!.Value) / 60.0;

        // 2. السجلات التي لا تحتوي على WorkingMinutes (نسي الموظف تسجيل الخروج)
        // استخدم ساعات الوردية المتوقعة أو احسب من وقت الدخول إلى نهاية الوردية
        var attendancesWithoutCheckOut = attendancesWithCheckIn
            .Where(a => !a.WorkingMinutes.HasValue && a.CheckInTime.HasValue)
            .ToList();

        foreach (Domain.Entities.Attendance.Attendance attendance in attendancesWithoutCheckOut)
        {
            int estimatedMinutes = 0;

            if (attendance.Shift is not null)
            {
                // احسب ساعات الوردية المتوقعة
                TimeOnly startTime = attendance.Shift.StartTime;
                TimeOnly endTime = attendance.Shift.EndTime;

                // التعامل مع الورديات التي تتجاوز منتصف الليل
                if (endTime < startTime)
                {
                    DateTime startDateTime = DateTime.Today.Add(startTime.ToTimeSpan());
                    DateTime endDateTime = DateTime.Today.AddDays(1).Add(endTime.ToTimeSpan());
                    estimatedMinutes = (int)(endDateTime - startDateTime).TotalMinutes;
                }
                else
                {
                    estimatedMinutes = (int)(endTime - startTime).TotalMinutes;
                }
            }
            else
            {
                // إذا لم تكن هناك وردية، استخدم القيمة الافتراضية (7 ساعات)
                estimatedMinutes = 420; // 7 ساعات × 60 دقيقة
            }

            totalWorkingHours += estimatedMinutes / 60.0;
        }

        int lateDays = employee.Attendances
            .Count(a => a.Status == Domain.Enums.AttendanceStatus.Late);

        int leaveDays = employee.Attendances
            .Count(a => a.Status == Domain.Enums.AttendanceStatus.Absent ||
                       a.Status == Domain.Enums.AttendanceStatus.Vacation);

        return Result.Success(new ApiResponse<GetEmployeeByIdVm>
        {
            Data = new GetEmployeeByIdVm
            {
                Id = employee.Id,
                EmpId = employee.EmpID,
                RFID = employee.RFID ?? string.Empty,
                UserId = employee.UserId ?? Guid.Empty,
                FirstName = employee.FirstName,
                SecondName = employee.SecondName,
                ThirdName = employee.ThirdName,
                FourthName = employee.FourthName,
                FamilyName = employee.FamilyName,
                FullName = employee.FullName,
                OrganizationalUnitId = employee.OrganizationalUnitId ?? Guid.Empty,
                OrganizationalUnitName = employee.OrganizationalUnit?.UnitName ?? string.Empty,
                ManagerId = employee.ManagerId,
                ManagerName = employee.Manager?.FullName,
                IsManager = employee.IsManager ?? false,
                CreatedAt = employee.CreatedAt,
                FaceImageUrl = employee.FaceImageUrl,
                NationalIdFrontUrl = employee.NationalIdFrontUrl,
                NationalIdBackUrl = employee.NationalIdBackUrl,
                ProfileImageUrl = employee.ProfileImageUrl,
                Status = employee.User?.Status ?? Domain.Enums.UserStatus.Inactive,
                StatusName = employee.User?.Status.ToString() ?? "Inactive",
                TotalWorkingDays = totalWorkingDays,
                TotalWorkingHours = totalWorkingHours,
                LateDays = lateDays,
                LeaveDays = leaveDays,
                AttendanceSchedules = attendanceScheduleDto,
                Attendances = attendanceDtos
            }
        });
    }
}
