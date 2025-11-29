using Application.Abstractions.Messaging;

namespace Application.Attendance.CheckOut;

public sealed class CheckOutCommand : ICommand<AttendanceResponse>
{
    public Guid EmployeeId { get; set; }
    public Guid AttendanceId { get; set; }
    public DateTime CheckOutTime { get; set; }
    public int? Major { get; set; }
    public int? Minor { get; set; }
    public string CardNo { get; set; } = string.Empty;
    public int? CardType { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? CardReaderNo { get; set; }
    public int? DoorNo { get; set; }
    public string? EmployeeNoString { get; set; }
    public int? SerialNo { get; set; }
    public string? UserType { get; set; }
    public string? CurrentVerifyMode { get; set; }
    public string? AttendanceStatus { get; set; } = "checkOut";
    public string? Label { get; set; }
    public string? Mask { get; set; }
    public string? PictureURL { get; set; }
    public string? Notes { get; set; }
}
