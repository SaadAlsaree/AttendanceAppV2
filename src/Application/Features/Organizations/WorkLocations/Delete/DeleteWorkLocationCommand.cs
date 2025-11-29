using Application.Abstractions.Messaging;

namespace Application.Organizations.WorkLocations.Delete;

public sealed record DeleteWorkLocationCommand(Guid Id) : ICommand;
