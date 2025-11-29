using Application.Abstractions.Messaging;

namespace Application.Organizations.Holidays.Update;

public sealed class UpdateHolidayCommand : ICommand
{
    public Guid HolidayId { get; set; }
    public string? Name { get; set; }
    public DateOnly? Date { get; set; }
    public bool? IsRecurring { get; set; }
}
