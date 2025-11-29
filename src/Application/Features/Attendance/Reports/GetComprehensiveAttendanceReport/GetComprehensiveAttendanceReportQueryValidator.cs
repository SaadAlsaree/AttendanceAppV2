using FluentValidation;

namespace Application.Attendance.Reports.GetComprehensiveAttendanceReport;

public class GetComprehensiveAttendanceReportQueryValidator : AbstractValidator<GetComprehensiveAttendanceReportQuery>
{
    public GetComprehensiveAttendanceReportQueryValidator()
    {
        RuleFor(c => c.StartDate)
            .NotEmpty()
            .WithMessage("تاريخ البداية مطلوب");

        RuleFor(c => c.EndDate)
            .NotEmpty()
            .WithMessage("تاريخ النهاية مطلوب");

        RuleFor(c => c.EndDate)
            .GreaterThan(c => c.StartDate)
            .WithMessage("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");

        RuleFor(c => c.ReportType)
            .IsInEnum()
            .WithMessage("نوع التقرير يجب أن يكون قيمة صحيحة");

        RuleFor(c => c.StartDate)
            .LessThanOrEqualTo(DateTime.Now.AddYears(1))
            .WithMessage("تاريخ البداية لا يمكن أن يكون في المستقبل البعيد");

        RuleFor(c => c.EndDate)
            .LessThanOrEqualTo(DateTime.Now.AddYears(1))
            .WithMessage("تاريخ النهاية لا يمكن أن يكون في المستقبل البعيد");

        RuleFor(c => c.StartDate)
            .GreaterThanOrEqualTo(DateTime.Now.AddYears(-5))
            .WithMessage("تاريخ البداية لا يمكن أن يكون أقدم من 5 سنوات");

        RuleFor(c => c.EndDate)
            .GreaterThanOrEqualTo(DateTime.Now.AddYears(-5))
            .WithMessage("تاريخ النهاية لا يمكن أن يكون أقدم من 5 سنوات");

        // التحقق من أن نطاق التاريخ معقول
        RuleFor(c => c.EndDate)
            .LessThanOrEqualTo(c => c.StartDate.AddDays(365))
            .WithMessage("نطاق التاريخ لا يمكن أن يتجاوز سنة واحدة");

        // التحقق من أن نطاق التاريخ لا يقل عن يوم واحد
        RuleFor(c => c.EndDate)
            .GreaterThanOrEqualTo(c => c.StartDate.AddDays(1))
            .WithMessage("نطاق التاريخ يجب أن يكون يوم واحد على الأقل");
    }
}
