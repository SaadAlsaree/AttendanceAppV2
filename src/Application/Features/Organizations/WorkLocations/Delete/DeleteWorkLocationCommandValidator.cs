using FluentValidation;

namespace Application.Organizations.WorkLocations.Delete;

public class DeleteWorkLocationCommandValidator : AbstractValidator<DeleteWorkLocationCommand>
{
    public DeleteWorkLocationCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
