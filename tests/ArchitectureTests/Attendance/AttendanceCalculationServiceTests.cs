using Application;
using Application.Attendance.Shared;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;
using Shouldly;

namespace ArchitectureTests.Attendance;

public sealed class AttendanceCalculationServiceTests
{
    private readonly IAttendanceCalculationService _calculationService = CreateCalculationService();

    [Fact]
    public void CalculateMetrics_ShouldCountOvernightMinutes_WhenCheckOutIsAfterMidnight()
    {
        Shift shift = CreateShift(new TimeOnly(20, 0), new TimeOnly(8, 0), ShiftType.Night);
        DateTime checkInUtc = new(2026, 6, 22, 17, 0, 0, DateTimeKind.Utc);
        DateTime checkOutUtc = new(2026, 6, 23, 5, 30, 0, DateTimeKind.Utc);

        AttendanceMetrics metrics = _calculationService.CalculateMetrics(checkInUtc, checkOutUtc, shift);

        metrics.WorkingMinutes.ShouldBe(750);
        metrics.OvertimeMinutes.ShouldBe(30);
    }

    [Fact]
    public void CalculateMetrics_ShouldTreatEarlierCheckOutAsNextDay_ForOvernightShift()
    {
        Shift shift = CreateShift(new TimeOnly(20, 0), new TimeOnly(8, 0), ShiftType.Night);
        DateTime checkInUtc = new(2026, 6, 22, 17, 0, 0, DateTimeKind.Utc);
        DateTime checkOutUtc = new(2026, 6, 22, 5, 0, 0, DateTimeKind.Utc);

        AttendanceMetrics metrics = _calculationService.CalculateMetrics(checkInUtc, checkOutUtc, shift);

        metrics.WorkingMinutes.ShouldBe(720);
        metrics.OvertimeMinutes.ShouldBe(0);
    }

    [Fact]
    public void CalculateMetrics_ShouldKeepNormalShiftDurationUnchanged()
    {
        Shift shift = CreateShift(new TimeOnly(8, 0), new TimeOnly(16, 0), ShiftType.Morning);
        DateTime checkInUtc = new(2026, 6, 22, 5, 0, 0, DateTimeKind.Utc);
        DateTime checkOutUtc = new(2026, 6, 22, 13, 0, 0, DateTimeKind.Utc);

        AttendanceMetrics metrics = _calculationService.CalculateMetrics(checkInUtc, checkOutUtc, shift);

        metrics.WorkingMinutes.ShouldBe(480);
        metrics.OvertimeMinutes.ShouldBe(0);
    }

    [Fact]
    public void CalculateMetrics_ShouldReturnZeroWorkingMinutes_WhenNormalShiftCheckOutIsBeforeCheckIn()
    {
        Shift shift = CreateShift(new TimeOnly(8, 0), new TimeOnly(16, 0), ShiftType.Morning);
        DateTime checkInUtc = new(2026, 6, 22, 10, 0, 0, DateTimeKind.Utc);
        DateTime checkOutUtc = new(2026, 6, 22, 9, 0, 0, DateTimeKind.Utc);

        AttendanceMetrics metrics = _calculationService.CalculateMetrics(checkInUtc, checkOutUtc, shift);

        metrics.WorkingMinutes.ShouldBe(0);
        metrics.OvertimeMinutes.ShouldBe(0);
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

    private static Shift CreateShift(TimeOnly startTime, TimeOnly endTime, ShiftType shiftType)
    {
        return new Shift
        {
            Name = shiftType.ToString(),
            StartTime = startTime,
            EndTime = endTime,
            ShiftType = shiftType,
            IsActive = true
        };
    }

    private sealed class BaghdadDateTimeProvider : IDateTimeProvider
    {
        private static readonly TimeZoneInfo BaghdadTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Baghdad");

        public DateTime Now => GetCurrentLocalTime();

        public DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }

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
