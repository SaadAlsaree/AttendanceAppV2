namespace Application.Features.Organizations.EmployeeWeeklyShifts.Get;

/// <summary>
/// One row per employee that has a fixed weekly pattern (تثبيت الدوام),
/// with the full pattern grouped under <see cref="Days"/>.
/// </summary>
public sealed class EmployeeWeeklyShiftsResponse
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string EmpId { get; set; } = string.Empty;
    public string? OrganizationalUnitName { get; set; }
    public List<WeeklyShiftDayResponse> Days { get; set; } = new List<WeeklyShiftDayResponse>();
}

public sealed class WeeklyShiftDayResponse
{
    /// <summary>.NET convention: Sunday = 0 … Saturday = 6.</summary>
    public int DayOfWeek { get; set; }
    public Guid ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
