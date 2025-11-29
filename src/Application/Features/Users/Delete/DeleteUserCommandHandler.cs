using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Users;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Users.Delete;

internal sealed class DeleteUserCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<DeleteUserCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<bool>(UserErrors.NotFound(command.UserId));
        }

        // Check if user is not trying to delete themselves
        if (user.Id == command.DeletedBy)
        {
            return Result.Failure<bool>(UserErrors.CannotDeleteSelf());
        }

        // Check if user has any associated data that would prevent deletion
        bool hasAssociatedData = await context.Employees
            .AnyAsync(e => e.UserId == command.UserId, cancellationToken);

        if (hasAssociatedData)
        {
            return Result.Failure<bool>(UserErrors.HasAssociatedData(command.UserId));
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
