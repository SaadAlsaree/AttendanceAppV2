using Application.Abstractions.Messaging;

namespace Application.Attendance.AttendanceSchedules.Update;

public sealed class UpdateAttendanceScheduleCommand : ICommand<AttendanceScheduleResponse>
{
    public Guid AttendanceScheduleId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Domain.Enums.ScheduleType ScheduleType { get; set; } = Domain.Enums.ScheduleType.Regular;
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
    public List<DateOnly>? ExcludedDates { get; set; }
    public List<UpdateScheduleDayCommand>? ScheduleDays { get; set; }

    /// <summary>
    /// Indicates whether ScheduleDays should be updated.
    /// If false, ScheduleDays will remain unchanged regardless of the ScheduleDays property value.
    /// If true, ScheduleDays will be updated based on the ScheduleDays property value.
    /// </summary>
    public bool UpdateScheduleDays { get; set; }
}


public sealed class UpdateScheduleDayCommand
{
    public Guid? Id { get; set; }
    public Guid? AttendanceScheduleId { get; set; }
    public DateOnly? ScheduleDayDate { get; set; }
    public Guid? ShiftId { get; set; }
    public bool? IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

