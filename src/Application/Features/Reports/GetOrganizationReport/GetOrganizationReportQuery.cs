
using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Reports.GetOrganizationReport;

public class GetOrganizationReportQuery : IQuery<ApiResponse<GetOrganizationReportVm>>
{
    public Guid OrganizationalUnitId { get; set; }
    public DateOnly? Date { get; set; }
    public Guid? ShiftId { get; set; }
    public bool IncludeSubUnits { get; set; } = true;
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
