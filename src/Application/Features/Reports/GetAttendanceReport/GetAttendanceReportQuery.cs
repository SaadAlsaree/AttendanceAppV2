using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Reports.GetAttendanceReport;

public sealed class GetAttendanceReportQuery : IQuery<ApiResponse<AttendanceReportVm>>
{
    public Guid OrganizationalUnitId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ShiftId { get; set; }
    public bool IncludeSubUnits { get; set; } = true;
}
