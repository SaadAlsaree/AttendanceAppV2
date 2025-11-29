using System;
using Application.Abstractions.Messaging;

namespace Application.Features.Attendance.AttendanceSchedules.BulkInsert;

public class BulkInsertAttendanceScheduleCommand : ICommand<bool>
{
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid ShiftId { get; set; }
}
