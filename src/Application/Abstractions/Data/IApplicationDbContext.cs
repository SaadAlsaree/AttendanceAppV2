using Domain.Entities.Devices;
using Domain.Entities.Organizations;
using Domain.Entities.Users;
using Domain.Todos;
using Microsoft.EntityFrameworkCore;
using AttendanceEntity = Domain.Entities.Attendance.Attendance;
using AttendanceLog = Domain.Entities.Attendance.AttendanceLog;
using AttendanceBreak = Domain.Entities.Attendance.AttendanceBreak;
using AttendanceSchedule = Domain.Entities.Attendance.AttendanceSchedule;
using ScheduleIssue = Domain.Entities.Attendance.ScheduleIssue;
using SecurityAuditLog = Domain.Entities.Attendance.SecurityAuditLog;
using Domain.Entities.Attendance;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TodoItem> TodoItems { get; }
    DbSet<Employee> Employees { get; }
    DbSet<OrganizationalUnit> OrganizationalUnits { get; }
    DbSet<WorkLocation> WorkLocations { get; }
    DbSet<Shift> Shifts { get; }
    DbSet<Holiday> Holidays { get; }
    DbSet<AttendanceSchedule> AttendanceSchedules { get; }
    DbSet<AttendanceEntity> Attendances { get; }
    DbSet<AttendanceLog> AttendanceLogs { get; }
    DbSet<AttendanceBreak> AttendanceBreaks { get; }
    DbSet<ScheduleIssue> ScheduleIssues { get; }
    DbSet<SecurityAuditLog> SecurityAuditLogs { get; }
    DbSet<Device> Devices { get; }
    DbSet<Domain.Entities.Attendance.Leave> Leaves { get; }
    DbSet<Attachment> Attachments { get; }

    DbSet<ScheduleDay> ScheduleDays { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
