using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.WorkLocations.Delete;

internal sealed class DeleteWorkLocationCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<DeleteWorkLocationCommand>
{
    public async Task<Result> Handle(DeleteWorkLocationCommand command, CancellationToken cancellationToken)
    {
        WorkLocation? workLocation = await context.WorkLocations
            .SingleOrDefaultAsync(wl => wl.Id == command.Id, cancellationToken);

        if (workLocation is null)
        {
            return Result.Failure(WorkLocationErrors.NotFound(command.Id));
        }




        context.WorkLocations.Remove(workLocation);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
