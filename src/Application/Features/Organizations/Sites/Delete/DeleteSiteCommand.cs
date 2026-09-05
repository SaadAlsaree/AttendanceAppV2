using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Sites.Delete;

public sealed record DeleteSiteCommand(Guid Id) : ICommand;
