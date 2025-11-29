using FluentValidation;

namespace Application.Features.Organizations.Employees.AssignManager;

public class AssignManagerCommandValidator : AbstractValidator<AssignManagerCommand>
{
    public AssignManagerCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.ManagerId).NotEmpty();

    }
}
