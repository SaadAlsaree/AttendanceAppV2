using FluentValidation;

namespace Application.Features.Organizations.Employees.Registration;

public class RegisterEmployeeCommandValidator : AbstractValidator<RegisterEmployeeCommand>
{
    public RegisterEmployeeCommandValidator()
    {
        // Employee validation rules
        // Make `Code` optional but limit its length when present.
        RuleFor(c => c.EmpId).MaximumLength(50);
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.SecondName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.ThirdName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.FourthName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.FamilyName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.RFID).NotEmpty().MaximumLength(50);
        RuleFor(c => c.OrganizationalUnitId).NotEmpty();
    }
}
