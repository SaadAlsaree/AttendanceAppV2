using Application.Abstractions.Messaging;

namespace Application.Organizations.Holidays.Create;

public sealed class CreateHolidayCommand : ICommand<Guid>
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public bool IsRecurring { get; set; }
}
