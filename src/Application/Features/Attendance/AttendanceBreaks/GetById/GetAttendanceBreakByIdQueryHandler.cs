using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceBreaks.Get;
using Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceBreaks.GetById;

internal sealed class GetAttendanceBreakByIdQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetAttendanceBreakByIdQuery, AttendanceBreakResponse>
{
    public async Task<Result<AttendanceBreakResponse>> Handle(GetAttendanceBreakByIdQuery query, CancellationToken cancellationToken)
    {
        AttendanceBreak? attendanceBreak = await context.AttendanceBreaks
            .Include(b => b.Attendance)
            .ThenInclude(a => a.Employee)
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == query.AttendanceBreakId, cancellationToken);

        if (attendanceBreak is null)
        {
            return Result.Failure<AttendanceBreakResponse>(AttendanceBreakErrors.NotFound(query.AttendanceBreakId));
        }

        var response = new AttendanceBreakResponse
        {
            Id = attendanceBreak.Id,
            AttendanceId = attendanceBreak.AttendanceId,
            EmployeeId = attendanceBreak.Attendance.EmployeeId,
            EmployeeName = attendanceBreak.Attendance.Employee.FirstName + " " + attendanceBreak.Attendance.Employee.FamilyName,
            StartTime = attendanceBreak.StartTime,
            EndTime = attendanceBreak.EndTime,
            DurationMinutes = attendanceBreak.DurationMinutes,
            BreakType = attendanceBreak.BreakType,
            Notes = attendanceBreak.Notes,
            CreatedAt = attendanceBreak.CreatedAt,
            LastUpdatedAt = attendanceBreak.LastUpdatedAt,
            AttendanceDate = attendanceBreak.Attendance.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
        };

        return response;
    }
}
