using FluentValidation;

namespace Application.Attendance.Leaves.Delete;

public class DeleteLeaveCommandValidator : AbstractValidator<DeleteLeaveCommand>
{
    public DeleteLeaveCommandValidator()
    {
        RuleFor(c => c.LeaveId)
            .NotEmpty()
            .WithMessage("Leave ID is required");
    }
}
