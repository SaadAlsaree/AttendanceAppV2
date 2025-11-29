using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Reports.GetOrganizationalSummary;

public sealed class GetOrganizationalSummaryQuery : IQuery<ApiResponse<OrganizationalSummaryVm>>
{
    public Guid? OrganizationalUnitId { get; set; }
    public bool IncludeSubUnits { get; set; } = true;
    public DateTime? Date { get; set; }
}

