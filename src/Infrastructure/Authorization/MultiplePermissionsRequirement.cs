using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Authorization;

public sealed class MultiplePermissionsRequirement : IAuthorizationRequirement
{
    public MultiplePermissionsRequirement(params string[] permissions)
    {
        Permissions = permissions;
    }

    public string[] Permissions { get; }
}

