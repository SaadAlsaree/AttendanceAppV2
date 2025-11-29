using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Attendance.AttendanceSchedules.Create;

public sealed class CreateAttendanceScheduleCommand : ICommand<bool>
{
    public Guid EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ScheduleType ScheduleType { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public List<CreateScheduleDayCommand> ScheduleDays { get; set; } = new List<CreateScheduleDayCommand>();
    public List<DateOnly> ExcludedDates { get; set; } = new List<DateOnly>();
}


public sealed class CreateScheduleDayCommand
{
    public Guid AttendanceScheduleId { get; set; }
    public DateOnly ScheduleDayDate { get; set; }
    public Guid ShiftId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
