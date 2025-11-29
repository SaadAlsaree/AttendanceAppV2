using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Employees.Profile;

internal sealed class GetProfileQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetProfileQuery, ProfileResponse>
{
    public async Task<Result<ProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        // Get current user's employee ID from the context
        if (request.Id == Guid.Empty)
        {
            return Result.Failure<ProfileResponse>(new Error(
                "Profile.NotFound",
                "Employee profile not found",
                ErrorType.Conflict));
        }

        // Query the employee with related data
        Employee employee = await context.Employees
            .Include(e => e.Manager)
            .Include(e => e.OrganizationalUnit)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<ProfileResponse>(new Error(
                "Profile.NotFound",
                "Employee profile not found",
                ErrorType.Conflict));
        }

        // Map to response
        var response = new ProfileResponse(
            employee.Id,
            employee.EmpID,
            employee.Code ?? string.Empty,
            employee.RFID ?? string.Empty,
            employee.FirstName,
            employee.SecondName,
            employee.ThirdName,
            employee.FourthName,
            employee.FamilyName ?? string.Empty,
            employee.FullName,
            employee.Email ?? string.Empty,
            employee.ManagerId,
            employee.Manager?.FullName,
            employee.OrganizationalUnitId,
            employee.OrganizationalUnit?.UnitName,
            employee.ProfileImageUrl,
            employee.CreatedAt);

        return response;
    }
}
