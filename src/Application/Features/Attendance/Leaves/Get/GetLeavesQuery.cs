using Application.Abstractions.Messaging;
using Domain.Enums;
using SharedKernel;

namespace Application.Attendance.Leaves.Get;

public sealed class GetLeavesQuery : IQuery<PaginatedResponse<LeaveResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? EmployeeId { get; set; }
    public Guid? ManagerId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public LeaveType? LeaveType { get; set; }
    public LeaveStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public SortOrder SortOrder { get; set; } = SortOrder.Ascending;
}

public enum SortOrder
{
    Ascending,
    Descending
}
