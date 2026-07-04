using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Employees.AssignWeeklyShifts;

public sealed class AssignWeeklyShiftsCommand : ICommand
{
    public Guid Id { get; set; }

    /// <summary>
    /// The employee's full weekly pattern (full-replace semantics):
    /// every submitted day gets the given shift, any weekday not present becomes a day off,
    /// and an empty list clears the whole pattern.
    /// </summary>
    public List<WeeklyShiftDay> Days { get; set; } = new List<WeeklyShiftDay>();
}

public sealed class WeeklyShiftDay
{
    /// <summary>.NET convention: Sunday = 0 … Saturday = 6.</summary>
    public int DayOfWeek { get; set; }
    public Guid ShiftId { get; set; }
}
