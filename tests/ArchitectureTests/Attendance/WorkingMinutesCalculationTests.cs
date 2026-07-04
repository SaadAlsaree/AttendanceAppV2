using Application;
using Application.Attendance.Shared;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace ArchitectureTests.Attendance;

public sealed class WorkingMinutesCalculationTests
{
    private readonly IAttendanceCalculationService _calculationService = CreateCalculationService();

    [Fact]
    public void CalculateWorkingMinutes_ShouldReturnElapsedMinutes_ForSameDayPair()
    {
        DateTime checkInUtc = new(2026, 6, 22, 5, 0, 0, DateTimeKind.Utc);
        DateTime checkOutUtc = new(2026, 6, 22, 13, 30, 0, DateTimeKind.Utc);

        int? workingMinutes = _calculationService.CalculateWorkingMinutes(checkInUtc, checkOutUtc);

        Assert.Equal(510, workingMinutes);
    }

    [Fact]
    public void CalculateWorkingMinutes_ShouldCountAcrossMidnight()
    {
        DateTime checkInUtc = new(2026, 6, 22, 20, 0, 0, DateTimeKind.Utc);
        DateTime checkOutUtc = new(2026, 6, 23, 4, 0, 0, DateTimeKind.Utc);

        int? workingMinutes = _calculationService.CalculateWorkingMinutes(checkInUtc, checkOutUtc);

        Assert.Equal(480, workingMinutes);
    }

    [Fact]
    public void CalculateWorkingMinutes_ShouldReturnNull_WhenTimestampsAreEqual()
    {
        DateTime scanUtc = new(2026, 6, 22, 8, 0, 0, DateTimeKind.Utc);

        int? workingMinutes = _calculationService.CalculateWorkingMinutes(scanUtc, scanUtc);

        Assert.Null(workingMinutes);
    }

    [Fact]
    public void CalculateWorkingMinutes_ShouldReturnNull_WhenCheckOutIsBeforeCheckIn()
    {
        DateTime checkInUtc = new(2026, 6, 22, 10, 0, 0, DateTimeKind.Utc);
        DateTime checkOutUtc = new(2026, 6, 22, 9, 0, 0, DateTimeKind.Utc);

        int? workingMinutes = _calculationService.CalculateWorkingMinutes(checkInUtc, checkOutUtc);

        Assert.Null(workingMinutes);
    }

    [Fact]
    public void CalculateWorkingMinutes_ShouldTruncateSubMinutePositiveSpanToZero()
    {
        DateTime checkInUtc = new(2026, 6, 22, 8, 0, 0, DateTimeKind.Utc);
        DateTime checkOutUtc = new(2026, 6, 22, 8, 0, 40, DateTimeKind.Utc);

        int? workingMinutes = _calculationService.CalculateWorkingMinutes(checkInUtc, checkOutUtc);

        Assert.Equal(0, workingMinutes);
    }

    [Fact]
    public void CalculateWorkingMinutes_ShouldNormalizeMixedKindsBeforeSubtracting()
    {
        // نفس اللحظة: 08:00 UTC == 11:00 بتوقيت بغداد (Unspecified يعامل كتوقيت بغداد)
        DateTime checkInBaghdad = new(2026, 6, 22, 11, 0, 0, DateTimeKind.Unspecified);
        DateTime checkOutUtc = new(2026, 6, 22, 16, 0, 0, DateTimeKind.Utc);

        int? workingMinutes = _calculationService.CalculateWorkingMinutes(checkInBaghdad, checkOutUtc);

        Assert.Equal(480, workingMinutes);
    }

    private static IAttendanceCalculationService CreateCalculationService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDateTimeProvider>(new BaghdadDateTimeProvider());
        services.AddApplication();

        return services
            .BuildServiceProvider()
            .GetRequiredService<IAttendanceCalculationService>();
    }

    private sealed class BaghdadDateTimeProvider : IDateTimeProvider
    {
        private static readonly TimeZoneInfo BaghdadTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Baghdad");

        public DateTime Now => GetCurrentLocalTime();

        public DateTime GetUtcNow() => DateTime.UtcNow;

        public DateTime ConvertToLocalTime(DateTime utcTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), BaghdadTimeZone);
        }

        public DateTime ConvertToUtc(DateTime localTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(localTime, BaghdadTimeZone);
        }

        public DateTime EnsureUtc(DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Utc
                ? dateTime
                : TimeZoneInfo.ConvertTimeToUtc(dateTime, BaghdadTimeZone);
        }

        public DateTime GetCurrentLocalTime()
        {
            return ConvertToLocalTime(DateTime.UtcNow);
        }

        public string GetTimeZoneId()
        {
            return BaghdadTimeZone.Id;
        }
    }
}
