using SharedKernel;

namespace Domain.Reports;

public static class ReportErrors
{
    public static Error InvalidDateRange(DateTime startDate, DateTime endDate) => Error.Problem(
        "Reports.InvalidDateRange",
        $"Invalid date range: Start date '{startDate:yyyy-MM-dd}' must be before end date '{endDate:yyyy-MM-dd}'");

    public static Error NoDataFound(DateTime startDate, DateTime endDate) => Error.NotFound(
        "Reports.NoDataFound",
        $"No report data found for the date range from '{startDate:yyyy-MM-dd}' to '{endDate:yyyy-MM-dd}'");

    public static Error Unauthorized() => Error.Forbidden(
        "Reports.Unauthorized",
        "You do not have permission to generate this report");

    public static Error InvalidExportFormat(string format) => Error.Problem(
        "Reports.InvalidExportFormat",
        $"The export format '{format}' is not supported");
}
