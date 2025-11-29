using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Shifts.Delete;

public sealed class DeleteShiftCommand : ICommand
{
    public Guid ShiftId { get; set; }
}
