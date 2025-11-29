using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Organizations.WorkLocations.Get;

public sealed record GetWorkLocationsQuery(Guid OrganizationId, int PageNumber = 1, int PageSize = 10) : IQuery<PaginatedResponse<WorkLocationResponse>>;
