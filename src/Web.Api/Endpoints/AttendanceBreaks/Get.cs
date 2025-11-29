using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceBreaks.Get;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AttendanceBreaks;

internal sealed class Get : IEndpoint
{
    public sealed class Request
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? EmployeeId { get; set; }
        public Guid? AttendanceId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? BreakType { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("attendance-breaks", async (
            [AsParameters] Request request,
            IQueryHandler<GetAttendanceBreaksQuery, PaginatedResponse<AttendanceBreakResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAttendanceBreaksQuery
            {
                Page = request.Page,
                PageSize = request.PageSize,
                EmployeeId = request.EmployeeId,
                AttendanceId = request.AttendanceId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                BreakType = request.BreakType != null ? Enum.Parse<Domain.Enums.BreakType>(request.BreakType) : null,
                SearchTerm = request.SearchTerm,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder != null ? Enum.Parse<SortOrder>(request.SortOrder) : null
            };

            Result<PaginatedResponse<AttendanceBreakResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceBreaks)
        .RequireAuthorization();
    }
}
