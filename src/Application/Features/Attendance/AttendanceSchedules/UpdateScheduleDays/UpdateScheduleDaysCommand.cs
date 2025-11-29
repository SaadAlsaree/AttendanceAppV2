using Application.Abstractions.Messaging;

namespace Application.Features.Attendance.AttendanceSchedules.UpdateScheduleDays;
public class UpdateScheduleDaysCommand : ICommand<bool>
{
    public Guid AttendanceScheduleId { get; set; }
    public List<UpdateScheduleDayCommand> ScheduleDays { get; set; } = new List<UpdateScheduleDayCommand>();
}

public class UpdateScheduleDayCommand
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}
