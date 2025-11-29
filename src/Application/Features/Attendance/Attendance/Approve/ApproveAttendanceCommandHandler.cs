using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Entities.Users;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Approve;

internal sealed class ApproveAttendanceCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ApproveAttendanceCommand, AttendanceResponse>
{
    public async Task<Result<AttendanceResponse>> Handle(ApproveAttendanceCommand command, CancellationToken cancellationToken)
    {
        Domain.Entities.Attendance.Attendance? attendance = await context.Attendances
            .Include(a => a.Employee)
            .Include(a => a.Shift)
            .SingleOrDefaultAsync(a => a.Id == command.AttendanceId, cancellationToken);

        if (attendance is null)
        {
            return Result.Failure<AttendanceResponse>(AttendanceErrors.NotFound(command.AttendanceId));
        }

        // Check if already approved
        if (attendance.ApprovedBy.HasValue)
        {
            return Result.Failure<AttendanceResponse>(AttendanceErrors.AlreadyApproved(command.AttendanceId));
        }

        // Validate approver exists
        User? approver = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.ApprovedBy, cancellationToken);

        if (approver is null)
        {
            return Result.Failure<AttendanceResponse>(UserErrors.NotFound(command.ApprovedBy));
        }

        // Approve the attendance record
        attendance.ApprovedBy = command.ApprovedBy;
        attendance.ApprovedAt = dateTimeProvider.GetUtcNow();
        attendance.LastUpdatedAt = dateTimeProvider.GetUtcNow();

        await context.SaveChangesAsync(cancellationToken);

        // Reload attendance with navigation properties for response
        attendance = await context.Attendances
            .Include(a => a.Employee)
            .Include(a => a.Shift)
            .AsNoTracking()
            .SingleAsync(a => a.Id == command.AttendanceId, cancellationToken);

        var response = new AttendanceResponse
        {
            Id = attendance.Id,
            EmployeeId = attendance.EmployeeId,
            OrganizationId = attendance.OrganizationId,
            Date = attendance.Date,
            CheckInTime = attendance.CheckInTime,
            CheckOutTime = attendance.CheckOutTime,
            Status = attendance.Status,
            ShiftId = attendance.ShiftId,
            WorkingMinutes = attendance.WorkingMinutes,
            BreakMinutes = attendance.BreakMinutes,
            OvertimeMinutes = attendance.OvertimeMinutes,
            LateMinutes = attendance.LateMinutes,
            EarlyLeaveMinutes = attendance.EarlyLeaveMinutes,
            Notes = attendance.Notes,
            CheckInMethod = attendance.CheckInMethod,
            CheckOutMethod = attendance.CheckOutMethod,
            ApprovedBy = attendance.ApprovedBy,
            ApprovedAt = attendance.ApprovedAt,
            AttendanceScheduleId = attendance.AttendanceScheduleId,
            CreatedAt = attendance.CreatedAt,
            UpdatedAt = attendance.LastUpdatedAt,
            FullName = attendance.Employee.FullName,
            Code = attendance.Employee.Code,
            ShiftName = attendance.Shift?.Name,
            ApproverName = approver.UserLogin
        };

        return response;
    }
}
