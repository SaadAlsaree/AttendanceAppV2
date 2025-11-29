using FluentValidation;

namespace Application.Features.Organizations.Employees.Delete;

public class DeleteEmployeeCommandValidator : AbstractValidator<DeleteEmployeeCommand>
{
    public DeleteEmployeeCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
