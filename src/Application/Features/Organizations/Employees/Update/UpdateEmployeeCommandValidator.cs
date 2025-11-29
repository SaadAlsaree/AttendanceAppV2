using FluentValidation;

namespace Application.Features.Organizations.Employees.Update;

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        // Employee validation rules
        RuleFor(c => c.EmpId).MaximumLength(50);
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.SecondName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.ThirdName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.FourthName).MaximumLength(50);
        RuleFor(c => c.FamilyName).MaximumLength(50);
        RuleFor(c => c.RFID).NotEmpty().MaximumLength(50);
        RuleFor(c => c.OrganizationalUnitId).NotEmpty();
    }
}
