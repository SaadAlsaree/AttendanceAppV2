using Domain.Entities.Organizations;
using Domain.Entities.Attendance;
using SharedKernel;

namespace Application.Attendance.Shared;

internal sealed class AttendanceCalculationService : IAttendanceCalculationService
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public AttendanceCalculationService(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }
    public AttendanceMetrics CalculateMetrics(
    DateTime checkInTime,
    DateTime checkOutTime,
    Shift shift)
    {
        // تحويل UTC إلى الوقت المحلي لحساب التأخير
        // النظام يعمل في توقيت بغداد (UTC+3)
        DateTime localCheckInTime = _dateTimeProvider.ConvertToLocalTime(checkInTime);
        DateTime localCheckOutTime = _dateTimeProvider.ConvertToLocalTime(checkOutTime);

        var checkInTimeOnly = TimeOnly.FromDateTime(localCheckInTime);
        var checkOutTimeOnly = TimeOnly.FromDateTime(localCheckOutTime);

        // Calculate late minutes
        int lateMinutes = CalculateLateMinutes(checkInTimeOnly, shift);

        // Calculate early leave minutes
        int earlyLeaveMinutes = CalculateEarlyLeaveMinutes(checkOutTimeOnly, shift);

        // Calculate working minutes (استخدام UTC للحساب الصحيح)
        int workingMinutes = CalculateWorkingMinutes(checkInTime, checkOutTime);

        // Calculate overtime minutes
        int overtimeMinutes = CalculateOvertimeMinutes(workingMinutes, shift);

        return new AttendanceMetrics(
            WorkingMinutes: workingMinutes,
            LateMinutes: lateMinutes,
            EarlyLeaveMinutes: earlyLeaveMinutes,
            OvertimeMinutes: overtimeMinutes);
    }

    public AttendanceMetrics CalculateMetricsWithLeave(
        DateTime checkInTime,
        DateTime checkOutTime,
        Shift shift,
        IEnumerable<AttendanceBreak> approvedLeaves)
    {
        // تحويل UTC إلى الوقت المحلي لحساب التأخير
        // النظام يعمل في توقيت بغداد (UTC+3)
        DateTime localCheckInTime = _dateTimeProvider.ConvertToLocalTime(checkInTime);
        DateTime localCheckOutTime = _dateTimeProvider.ConvertToLocalTime(checkOutTime);

        var checkInTimeOnly = TimeOnly.FromDateTime(localCheckInTime);
        var checkOutTimeOnly = TimeOnly.FromDateTime(localCheckOutTime);

        // Calculate late minutes with leave consideration
        int lateMinutes = CalculateLateMinutesWithLeave(checkInTimeOnly, shift, approvedLeaves);

        // Calculate early leave minutes
        int earlyLeaveMinutes = CalculateEarlyLeaveMinutes(checkOutTimeOnly, shift);

        // Calculate working minutes (excluding approved leave time)
        int workingMinutes = CalculateWorkingMinutes(checkInTime, checkOutTime);

        // Calculate overtime minutes
        int overtimeMinutes = CalculateOvertimeMinutes(workingMinutes, shift);

        return new AttendanceMetrics(
            WorkingMinutes: workingMinutes,
            LateMinutes: lateMinutes,
            EarlyLeaveMinutes: earlyLeaveMinutes,
            OvertimeMinutes: overtimeMinutes);
    }

    private static int CalculateLateMinutes(TimeOnly checkInTime, Shift shift)
    {
        TimeOnly expectedStartTime = shift.StartTime;
        int gracePeriod = shift.GracePeriodMinutes ?? 0;
        TimeOnly effectiveStartTime = expectedStartTime.AddMinutes(gracePeriod);

        // إذا كان وقت الدخول قبل وقت البدء المتوقع، فلا يوجد تأخير
        if (checkInTime <= effectiveStartTime)
        {
            return 0; // لا يوجد تأخير - وصل في الوقت أو مبكراً
        }

        // وقت الدخول بعد وقت البدء المتوقع - احسب التأخير
        return (int)(checkInTime - effectiveStartTime).TotalMinutes;
    }

    private static int CalculateLateMinutesWithLeave(TimeOnly checkInTime, Shift shift, IEnumerable<AttendanceBreak> approvedLeaves)
    {
        TimeOnly expectedStartTime = shift.StartTime;
        int gracePeriod = shift.GracePeriodMinutes ?? 0;
        TimeOnly effectiveStartTime = expectedStartTime.AddMinutes(gracePeriod);

        // Calculate total approved leave minutes that affect start time
        int approvedLeaveMinutes = CalculateApprovedLeaveMinutes(approvedLeaves, expectedStartTime);

        // Adjust effective start time by adding approved leave minutes
        TimeOnly adjustedStartTime = effectiveStartTime.AddMinutes(approvedLeaveMinutes);

        // إذا كان وقت الدخول قبل وقت البدء المتوقع، فلا يوجد تأخير
        if (checkInTime <= adjustedStartTime)
        {
            return 0; // لا يوجد تأخير - وصل في الوقت أو مبكراً
        }

        // وقت الدخول بعد وقت البدء المتوقع - احسب التأخير
        return (int)(checkInTime - adjustedStartTime).TotalMinutes;
    }

    private static int CalculateApprovedLeaveMinutes(IEnumerable<AttendanceBreak> approvedLeaves, TimeOnly shiftStartTime)
    {
        int totalLeaveMinutes = 0;

        foreach (AttendanceBreak leave in approvedLeaves)
        {
            // Only count leaves that start before or at shift start time
            var leaveStartTime = TimeOnly.FromDateTime(leave.StartTime);
            if (leaveStartTime <= shiftStartTime)
            {
                totalLeaveMinutes += leave.DurationMinutes;
            }
        }

        return totalLeaveMinutes;
    }

    private static int CalculateEarlyLeaveMinutes(TimeOnly checkOutTime, Shift shift)
    {
        TimeOnly expectedEndTime = shift.EndTime;
        const int gracePeriodMinutes = 10; // فترة سماحية 10 دقائق قبل وقت الانصراف المحدد

        // حساب وقت الانصراف الفعلي المسموح به (بعد طرح فترة السماحية)
        TimeOnly effectiveEndTime = expectedEndTime.AddMinutes(-gracePeriodMinutes);

        // التعامل مع الورديات التي تتجاوز منتصف الليل
        bool isNightShift = shift.EndTime < shift.StartTime;

        // في الورديات الليلية، إذا كان checkOutTime بعد منتصف الليل (أقل من StartTime)
        // و effectiveEndTime قبل منتصف الليل (أكبر من StartTime)،
        // هذا يعني أن checkOutTime في اليوم التالي، ولا يعتبر انصراف مبكر
        if (isNightShift && checkOutTime < shift.StartTime && effectiveEndTime >= shift.StartTime)
        {
            return 0;
        }

        // إذا كان وقت الانصراف قبل وقت الانصراف الفعلي المسموح به، يعتبر انصراف مبكر
        if (checkOutTime < effectiveEndTime)
        {
            // في حالة الورديات الليلية، قد نحتاج إلى إضافة 24 ساعة للفرق
            if (isNightShift && checkOutTime >= shift.StartTime && effectiveEndTime < shift.StartTime)
            {
                // checkOutTime قبل منتصف الليل، effectiveEndTime بعد منتصف الليل
                // هذا يعني أن effectiveEndTime في اليوم التالي
                return (int)((TimeOnly.MaxValue - checkOutTime).TotalMinutes +
                            (effectiveEndTime - TimeOnly.MinValue).TotalMinutes + 1);
            }

            return (int)(effectiveEndTime - checkOutTime).TotalMinutes;
        }

        return 0; // Not early - انصرف في الوقت المحدد أو بعده
    }

    private static int CalculateWorkingMinutes(DateTime checkInTime, DateTime checkOutTime)
    {
        return (int)(checkOutTime - checkInTime).TotalMinutes;
    }

    private static int CalculateOvertimeMinutes(int workingMinutes, Shift shift)
    {
        int expectedWorkingMinutes = CalculateExpectedWorkingMinutes(shift);

        if (workingMinutes > expectedWorkingMinutes)
        {
            return workingMinutes - expectedWorkingMinutes;
        }

        return 0; // No overtime
    }

    private static int CalculateExpectedWorkingMinutes(Shift shift)
    {
        TimeOnly startTime = shift.StartTime;
        TimeOnly endTime = shift.EndTime;

        // Handle shifts that span midnight
        if (endTime < startTime)
        {
            // Convert to DateTime for calculation, adding a day to end time
            DateTime startDateTime = DateTime.Today.Add(startTime.ToTimeSpan());
            DateTime endDateTime = DateTime.Today.AddDays(1).Add(endTime.ToTimeSpan());
            return (int)(endDateTime - startDateTime).TotalMinutes;
        }

        return (int)(endTime - startTime).TotalMinutes;
    }
}
