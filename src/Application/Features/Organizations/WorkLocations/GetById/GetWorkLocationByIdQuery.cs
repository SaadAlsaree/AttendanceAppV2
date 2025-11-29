using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Organizations.WorkLocations.GetById;

public sealed record GetWorkLocationByIdQuery(Guid WorkLocationId) : IQuery<ApiResponse<WorkLocationResponse>>;
