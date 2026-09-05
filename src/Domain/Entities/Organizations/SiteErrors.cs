using SharedKernel;

namespace Domain.Entities.Organizations;

public static class SiteErrors
{
    public static Error NotFound(Guid siteId) => Error.NotFound(
        "Site.NotFound",
        $"The site with the identifier {siteId} was not found");

    public static readonly Error SiteCodeAlreadyExists = Error.Conflict(
        "Site.SiteCodeAlreadyExists",
        "The site code already exists");

    public static readonly Error HasAssignedUnits = Error.Conflict(
        "Site.HasAssignedUnits",
        "Cannot delete a site that still has organizational units assigned to it");

    public static readonly Error HasAssignedUsers = Error.Conflict(
        "Site.HasAssignedUsers",
        "Cannot delete a site that still has users assigned to it");

    public static readonly Error AccessDenied = Error.Forbidden(
        "Site.AccessDenied",
        "You are not authorized to access this site");

    public static readonly Error NoSiteAssigned = Error.Forbidden(
        "Site.NoSiteAssigned",
        "No site is assigned to this user");
}
