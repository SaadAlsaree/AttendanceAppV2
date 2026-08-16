using Domain.Enums;

namespace Application.Features.Organizations.Employees.GetById;

public class GetEmployeeByIdVm
{
    public Guid Id { get; set; }
    public string RFID { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? FirstName { get; set; }
    public string? SecondName { get; set; }
    public string? ThirdName { get; set; }
    public string? FourthName { get; set; }
    public string? FamilyName { get; set; }
    public string EmpId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Guid OrganizationalUnitId { get; set; }
    public string OrganizationalUnitName { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsManager { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? FaceImageUrl { get; set; }
    public string? NationalIdFrontUrl { get; set; }
    public string? NationalIdBackUrl { get; set; }
    public string? ProfileImageUrl { get; set; }
    public UserStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;

    // إحصائيات العمل
    public int TotalWorkingDays { get; set; }
    public double TotalWorkingHours { get; set; }
    public int LateDays { get; set; }
    public int LeaveDays { get; set; }

    public AttendanceScheduleDto AttendanceSchedules { get; set; }
    public List<AttendanceDto> Attendances { get; set; } = new List<AttendanceDto>();

    // الدوام الثابت الأسبوعي
    public List<WeeklyShiftDto> WeeklyShifts { get; set; } = new List<WeeklyShiftDto>();
}

public class WeeklyShiftDto
{
    /// <summary>.NET convention: Sunday = 0 … Saturday = 6.</summary>
    public int DayOfWeek { get; set; }
    public Guid ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

public class AttendanceDto
{

    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public AttendanceStatus Status { get; set; }
    public Guid? ShiftId { get; set; }
    public int? WorkingMinutes { get; set; }
    public int? BreakMinutes { get; set; }
    public int? OvertimeMinutes { get; set; }
    public int? LateMinutes { get; set; }
    public int? EarlyLeaveMinutes { get; set; }
    public string? Notes { get; set; }
    public LogMethod? CheckInMethod { get; set; }
    public LogMethod? CheckOutMethod { get; set; }

}



public class AttendanceScheduleDto
{
    public Guid Id { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ScheduleType ScheduleType { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    public List<DateOnly> ExcludedDates { get; set; } = new List<DateOnly>();
    public ICollection<ScheduleIssueDto> Exceptions { get; set; } = new List<ScheduleIssueDto>();
    public ICollection<ScheduleDayDto> ScheduleDays { get; set; } = new List<ScheduleDayDto>();
}

public class ScheduleIssueDto
{

    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public Guid ShiftId { get; set; } // The different shift to apply on this date
    public string Reason { get; set; } = string.Empty;
    public ExceptionType ExceptionType { get; set; }
}

public class ScheduleDayDto
{

    public Guid AttendanceScheduleId { get; set; }
    public DateOnly ScheduleDayDate { get; set; }
    public Guid ShiftId { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}





