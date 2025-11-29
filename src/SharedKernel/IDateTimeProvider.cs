namespace SharedKernel;

/// <summary>
/// واجهة شاملة لخدمة الوقت والتوقيتات
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// الحصول على الوقت الحالي بالتوقيت المحلي (توقيت بغداد)
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// الحصول على الوقت الحالي بتوقيت UTC
    /// </summary>
    DateTime GetUtcNow();

    /// <summary>
    /// تحويل UTC إلى التوقيت المحلي
    /// </summary>
    DateTime ConvertToLocalTime(DateTime utcTime);

    /// <summary>
    /// تحويل التوقيت المحلي إلى UTC
    /// </summary>
    DateTime ConvertToUtc(DateTime localTime);

    /// <summary>
    /// تحويل DateTime إلى UTC مع التعامل مع DateTimeKind.Unspecified
    /// </summary>
    DateTime EnsureUtc(DateTime dateTime);

    /// <summary>
    /// الحصول على التوقيت المحلي الحالي
    /// </summary>
    DateTime GetCurrentLocalTime();

    /// <summary>
    /// الحصول على معرف المنطقة الزمنية
    /// </summary>
    string GetTimeZoneId();
}
