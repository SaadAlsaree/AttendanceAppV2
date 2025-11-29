using Application.Abstractions.Messaging;

namespace Application.Organizations.Holidays.Delete;

public sealed class DeleteHolidayCommand : ICommand
{
    public Guid HolidayId { get; set; }
}
