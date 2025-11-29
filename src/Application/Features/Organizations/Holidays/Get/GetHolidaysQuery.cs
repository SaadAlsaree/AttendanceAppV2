using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Organizations.Holidays.Get;

public sealed class GetHolidaysQuery : IQuery<PaginatedResponse<HolidayResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? OrganizationId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool? IsRecurring { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
}
