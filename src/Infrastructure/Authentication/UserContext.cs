using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Models;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authentication;

internal sealed class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationDbContext _context;

    public UserContext(IHttpContextAccessor httpContextAccessor, IApplicationDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public Guid UserId =>
        _httpContextAccessor
            .HttpContext?
            .User
            .GetUserId() ??
        throw new ApplicationException("User context is unavailable");

    public async Task<UserInfoDto> GetUserAsync()
    {
        UserInfoDto user = await _context.Users.Select(x => new UserInfoDto
        {
            Id = x.Id,
            Username = x.Username,
            UserLogin = x.UserLogin,
            Role = x.Role,
            RoleName = x.Role.ToString(),
            OrganizationalUnitId = x.OrganizationalUnitId,
            OrganizationalUnitName = x.OrganizationalUnit != null ? x.OrganizationalUnit.UnitName : string.Empty,
            SiteId = x.SiteId,
            SiteName = x.Site != null ? x.Site.SiteName : string.Empty,
            IsActive = x.IsActive,
            LastLoginDate = x.LastLoginDate,
            Status = x.Status
        }).FirstOrDefaultAsync(x => x.Id == UserId)
            ?? throw new ApplicationException("User not found");

        return user;
    }
}
