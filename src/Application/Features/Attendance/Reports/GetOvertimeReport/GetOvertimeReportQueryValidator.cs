using FluentValidation;
using Domain.Enums;

namespace Application.Attendance.Reports.GetOvertimeReport;

public class GetOvertimeReportQueryValidator : AbstractValidator<GetOvertimeReportQuery>
{
    public GetOvertimeReportQueryValidator()
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

        RuleFor(q => q.OvertimeType)
            .MaximumLength(50)
            .When(q => !string.IsNullOrEmpty(q.OvertimeType))
            .WithMessage("Overtime type cannot exceed 50 characters");

        RuleFor(q => q.GroupBy)
            .MaximumLength(50)
            .When(q => !string.IsNullOrEmpty(q.GroupBy))
            .WithMessage("Group by cannot exceed 50 characters");

        RuleFor(q => q.MinOvertimeHours)
            .GreaterThanOrEqualTo(0)
            .When(q => q.MinOvertimeHours.HasValue)
            .WithMessage("Minimum overtime hours must be greater than or equal to 0");

        RuleFor(q => q.ExportFormat)
            .IsInEnum()
            .When(q => q.ExportFormat.HasValue)
            .WithMessage("Export format must be a valid value");
    }
}
