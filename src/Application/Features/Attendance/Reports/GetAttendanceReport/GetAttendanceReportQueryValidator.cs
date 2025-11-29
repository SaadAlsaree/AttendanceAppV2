using FluentValidation;
using Domain.Enums;

namespace Application.Attendance.Reports.GetAttendanceReport;

public class GetAttendanceReportQueryValidator : AbstractValidator<GetAttendanceReportQuery>
{
    public GetAttendanceReportQueryValidator()
    {
        RuleFor(q => q.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required");

        RuleFor(q => q.EndDate)
            .NotEmpty()
            .WithMessage("End date is required");

        RuleFor(q => q.EndDate)
            .GreaterThan(q => q.StartDate)
            .WithMessage("End date must be after start date");

        RuleFor(q => q.ReportType)
            .IsInEnum()
            .WithMessage("Report type must be a valid value");

        RuleFor(q => q.GroupBy)
            .MaximumLength(50)
            .When(q => !string.IsNullOrEmpty(q.GroupBy))
            .WithMessage("Group by cannot exceed 50 characters");

        RuleFor(q => q.ExportFormat)
            .IsInEnum()
            .When(q => q.ExportFormat.HasValue)
            .WithMessage("Export format must be a valid value");
    }
}
