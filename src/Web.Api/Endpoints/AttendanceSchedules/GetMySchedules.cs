using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceSchedules.GetMySchedules;
using Domain.Enums;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance.AttendanceSchedules;

internal sealed class GetMySchedules : IEndpoint
{
    public sealed class Request
    {
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        public string? ScheduleType { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortOrder { get; set; } = "Descending";
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("attendance-schedules/my-schedules", async (
            [AsParameters] Request request,
            IQueryHandler<GetMySchedulesQuery, PaginatedResponse<AttendanceScheduleResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetMySchedulesQuery
            {
                Page = request.Page ?? 1,
                PageSize = request.PageSize ?? 10,
                ScheduleType = request.ScheduleType is not null ? Enum.Parse<ScheduleType>(request.ScheduleType) : null,
                IsActive = request.IsActive,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder
            };

            Result<PaginatedResponse<AttendanceScheduleResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceSchedules)
        .RequireAuthorization();
    }
}
