namespace Application.Features.Reports.GetOrganizationalSummary;

public sealed class OrganizationalSummaryVm
{
    public DateTime Date { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int TotaleEmployees { get; set; }
    public int TottalAttendances { get; set; }
    public int TottalNotAttendances { get; set; }
    public int TotalLate { get; set; }
    public int TotalLeaves { get; set; }
    public int TotalOvertime { get; set; }

    // ملخص الوحدات التنظيمية
    public List<UnitSummary> Units { get; set; } = new();

}

public sealed class UnitSummary
{
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public Guid? ParentUnitId { get; set; }
    public string? ParentUnitName { get; set; }

    // إحصائيات الوحدة
    public int TotalEmployees { get; set; }
    public int TotalShifts { get; set; }
    public int TottalAttendances { get; set; }
    public int TottalNotAttendances { get; set; }
    public int TotalLate { get; set; }
    public int TotalLeaves { get; set; }
    public int TotalOvertime { get; set; }
}


