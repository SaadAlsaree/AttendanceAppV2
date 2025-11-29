using SharedKernel;

namespace Domain.Entities.Users;

public static class PermissionErrors
{
    public static Error NotFound(Guid permissionId) => Error.NotFound(
        "Permissions.NotFound",
        $"The permission with ID '{permissionId}' was not found");

    public static Error NotFound(IEnumerable<Guid> permissionIds) => Error.NotFound(
        "Permissions.MultipleNotFound",
        $"The following permission IDs were not found: {string.Join(", ", permissionIds)}");

    public static readonly Error NameNotUnique = Error.Conflict(
        "Permissions.NameNotUnique",
        "A permission with this name already exists");

    public static Error InvalidResource(string resource) => Error.Failure(
        "Permissions.InvalidResource",
        $"The resource '{resource}' is not a valid permission resource");

    public static Error InvalidAction(string action) => Error.Failure(
        "Permissions.InvalidAction",
        $"The action '{action}' is not a valid permission action");

    public static Error AlreadyAssigned(Guid userId, Guid permissionId) => Error.Conflict(
        "Permissions.AlreadyAssigned",
        $"The permission with ID '{permissionId}' is already assigned to user with ID '{userId}'");

    public static Error NotAssigned(Guid userId, Guid permissionId) => Error.Failure(
        "Permissions.NotAssigned",
        $"The permission with ID '{permissionId}' is not assigned to user with ID '{userId}'");

    public static Error CannotModifySystemPermission() => Error.Failure(
        "Permissions.CannotModifySystemPermission",
        "System permissions cannot be modified or deleted");

    public static Error InactivePermission(Guid permissionId) => Error.Failure(
        "Permissions.InactivePermission",
        $"The permission with ID '{permissionId}' is inactive and cannot be assigned");
}
