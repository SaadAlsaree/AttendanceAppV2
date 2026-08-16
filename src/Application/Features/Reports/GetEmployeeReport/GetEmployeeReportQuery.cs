using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Reports.GetEmployeeReport;

// تقرير موظف واحد ضمن مدى زمني (من - إلى). متاح للأدمن فقط (يُفرض على مستوى نقطة النهاية).
public class GetEmployeeReportQuery : IQuery<ApiResponse<GetEmployeeReportVm>>
{
    public Guid EmployeeId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}
