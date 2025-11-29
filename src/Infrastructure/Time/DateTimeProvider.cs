using SharedKernel;

namespace Infrastructure.Time;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    private readonly string _timeZoneId;
    private readonly TimeZoneInfo _timeZone;

    public DateTimeProvider(string timeZoneId = "Asia/Baghdad")
    {
        _timeZoneId = timeZoneId;
        _timeZone = GetTimeZoneInfo(timeZoneId);
    }

    public DateTime Now => GetCurrentLocalTime();

    public DateTime GetUtcNow() => DateTime.UtcNow;

    public DateTime ConvertToLocalTime(DateTime utcTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _timeZone);
    }

    public DateTime ConvertToUtc(DateTime localTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localTime, _timeZone);
    }

    public DateTime EnsureUtc(DateTime dateTime)
    {
        switch (dateTime.Kind)
        {
            case DateTimeKind.Utc:
                return dateTime;

            case DateTimeKind.Local:
                // If Local, treat it as if it's in the configured timezone (Asia/Baghdad)
                // Convert from system local to UTC first, then treat as configured timezone
                DateTime utcFromSystemLocal = TimeZoneInfo.ConvertTimeToUtc(dateTime);
                // Now treat this UTC time as if it represents the same wall-clock time in configured timezone
                // Convert back to configured timezone to get the wall-clock time, then to UTC
                DateTime inConfiguredTz = TimeZoneInfo.ConvertTimeFromUtc(utcFromSystemLocal, _timeZone);
                var unspecified = DateTime.SpecifyKind(inConfiguredTz, DateTimeKind.Unspecified);
                return TimeZoneInfo.ConvertTimeToUtc(unspecified, _timeZone);

            case DateTimeKind.Unspecified:
                return TimeZoneInfo.ConvertTimeToUtc(dateTime, _timeZone);

            default:
                throw new ArgumentException($"Unexpected DateTimeKind: {dateTime.Kind}");
        }
    }

    public DateTime GetCurrentLocalTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
    }

    public string GetTimeZoneId()
    {
        return _timeZoneId;
    }

    /// <summary>
    /// الحصول على معلومات المنطقة الزمنية مع دعم الأنظمة المختلفة
    /// </summary>
    private static TimeZoneInfo GetTimeZoneInfo(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // محاولة باستخدام معرفات بديلة متوافقة مع أنظمة مختلفة
            return GetAlternativeTimeZone(timeZoneId);
        }
    }

    /// <summary>
    /// الحصول على منطقة زمنية بديلة متوافقة مع النظام
    /// </summary>
    private static TimeZoneInfo GetAlternativeTimeZone(string originalTimeZoneId)
    {
        // قائمة بالمناطق الزمنية البديلة لكل نظام
        var alternativeTimeZones = new Dictionary<string, string[]>
        {
            // Windows time zone IDs -> Linux/Mac time zone IDs
            ["Baghdad Standard Time"] = ["Asia/Baghdad"],
            ["Arab Standard Time"] = ["Asia/Riyadh"],
            ["Egypt Standard Time"] = ["Africa/Cairo"],
            ["Turkey Standard Time"] = ["Europe/Istanbul"],
            ["Central European Standard Time"] = ["Europe/Berlin", "Europe/Paris"],
            ["Eastern Standard Time"] = ["America/New_York"],
            ["Pacific Standard Time"] = ["America/Los_Angeles"],
            ["UTC"] = ["Etc/UTC"],

            // Linux/Mac time zone IDs -> Windows time zone IDs
            ["Asia/Baghdad"] = ["Baghdad Standard Time"],
            ["Asia/Riyadh"] = ["Arab Standard Time"],
            ["Africa/Cairo"] = ["Egypt Standard Time"],
            ["Europe/Istanbul"] = ["Turkey Standard Time"],
            ["Europe/Berlin"] = ["Central European Standard Time"],
            ["Europe/Paris"] = ["Central European Standard Time"],
            ["America/New_York"] = ["Eastern Standard Time"],
            ["America/Los_Angeles"] = ["Pacific Standard Time"],
            ["Etc/UTC"] = ["UTC"]
        };

        if (alternativeTimeZones.TryGetValue(originalTimeZoneId, out string[]? alternatives))
        {
            foreach (string alternative in alternatives)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(alternative);
                }
                catch (TimeZoneNotFoundException)
                {
                    // استمر في المحاولة مع البديل التالي
                    continue;
                }
            }
        }

        // إذا فشلت جميع المحاولات، استخدم المنطقة الزمنية المحلية للنظام
        try
        {
            return TimeZoneInfo.Local;
        }
        catch
        {
            // كملاذ أخير، استخدم UTC
            return TimeZoneInfo.Utc;
        }
    }
}
