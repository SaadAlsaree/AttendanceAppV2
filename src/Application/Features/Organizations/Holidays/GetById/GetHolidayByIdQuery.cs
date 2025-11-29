using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Organizations.Holidays.GetById;

public sealed record GetHolidayByIdQuery(Guid HolidayId) : IQuery<ApiResponse<HolidayResponse>>;
