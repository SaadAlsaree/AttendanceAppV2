using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Employees.Profile;

public sealed record GetProfileQuery(Guid Id) : IQuery<ProfileResponse>;
