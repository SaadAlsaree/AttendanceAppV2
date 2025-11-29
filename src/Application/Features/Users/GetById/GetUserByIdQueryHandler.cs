using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Users;
using Domain.Enums;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Users.GetById;

internal sealed class GetUserByIdQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetUserByIdQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .Include(u => u.OrganizationalUnit)
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserResponse>(UserErrors.NotFound(query.UserId));
        }

        // check if user role not Admin then apply accessible unit ids filter

        UserInfoDto userInfo = await userContext.GetUserAsync();

        if (userInfo.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            if (!accessibleUnitIds.Contains(user.OrganizationalUnitId!.Value))
            {
                return Result.Failure<UserResponse>(UserErrors.NotFound(query.UserId));
            }
        }

        var response = new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            UserLogin = user.UserLogin,
            Role = user.Role,
            Status = user.Status,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginDate = user.LastLoginDate,
            OrganizationalUnitId = user.OrganizationalUnitId,
            OrganizationalUnitName = user.OrganizationalUnit?.UnitName,
            OrganizationalUnitCode = user.OrganizationalUnit?.UnitCode
        };

        return response;
    }
}
