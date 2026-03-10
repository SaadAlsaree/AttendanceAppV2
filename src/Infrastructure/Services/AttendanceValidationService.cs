using Infrastructure.Database;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;


public class AttendanceValidationService : IAttendanceValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AttendanceValidationService> _logger;
    public AttendanceValidationService(ApplicationDbContext context, ILogger<AttendanceValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }



    public async Task<bool> IsDuplicateLogAsync(string empId, DateTime dateTime, string deviceNo)
    {
        try
        {
            // البحث عن سجل مطابق (نفس الموظف، نفس الوقت تقريباً، نفس الجهاز)
            // السماح بفارق ±30 ثانية
            var timeWindow = TimeSpan.FromSeconds(30);
            DateTime startTime = dateTime.Add(-timeWindow);
            DateTime endTime = dateTime.Add(timeWindow);

            bool exists = await _context.AttendanceLogs
                .AnyAsync(log =>
                    log.EmpID == empId &&
                    log.DateTimeAttend >= startTime &&
                    log.DateTimeAttend <= endTime &&
                    log.DeviceNo == deviceNo &&
                    !log.IsDeleted);

            if (exists)
            {
                //_logger.LogDebug(
                //    "Duplicate log found for EmpID {EmpID} at {DateTime} on Device {DeviceNo}",
                //    empId, dateTime, deviceNo);
            }

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate log for EmpID {EmpID}", empId);
            // في حالة الخطأ، نفترض أنه ليس مكرراً لتجنب فقدان البيانات
            return false;
        }
    }


}
