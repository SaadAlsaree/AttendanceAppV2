using SharedKernel;

namespace Domain.Common;

/// <summary>
/// Value object representing an Employee reference across different bounded contexts
/// </summary>
public sealed record EmployeeReference
{
    public Guid Id { get; init; }
    public string EmployeeNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    public static EmployeeReference Create(Guid id, string employeeNumber, string fullName, string email)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Employee ID cannot be empty", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            throw new ArgumentException("Employee number is required", nameof(employeeNumber));
        }

        return new EmployeeReference
        {
            Id = id,
            EmployeeNumber = employeeNumber,
            FullName = fullName,
            Email = email
        };
    }
}
