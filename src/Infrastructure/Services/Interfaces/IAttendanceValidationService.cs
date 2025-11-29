namespace Infrastructure.Services.Interfaces;
public interface IAttendanceValidationService
{

    /// <summary>
    /// التحقق من عدم وجود تسجيل مكرر
    /// </summary>
    Task<bool> IsDuplicateLogAsync(string empId, DateTime dateTime, string deviceNo);


}
/// <summary>
/// نتيجة عملية التحقق
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);

    public override string ToString()
    {
        return $"Valid: {IsValid} | Errors: {Errors.Count} | Warnings: {Warnings.Count}";
    }
}
