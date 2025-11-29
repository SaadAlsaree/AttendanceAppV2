using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Employees.Profile;

internal sealed class UpdateProfileCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateProfileCommand>
{
    public async Task<Result> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        // Get current user's employee ID from the context
        if (command.Id == Guid.Empty)
        {
            return Result.Failure(new Error(
                "Profile.NotFound",
                "Employee profile not found",
                ErrorType.Conflict));
        }

        // Get the employee
        Employee employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(new Error(
                "Profile.NotFound",
                "Employee profile not found",
                ErrorType.Conflict));
        }

        // Update the profile information
        employee.ProfileImageUrl = command.ProfileImage ?? string.Empty;
        employee.LastUpdatedAt = dateTimeProvider.Now;

        // Raise domain event if needed
        employee.Raise(new EmployeeUpdatedDomainEvent(employee.Id, employee.Code ?? string.Empty));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
