namespace Application.Abstractions.Authentication;

public interface IHasPermission
{


    /// <summary>
    /// Gets all organizational unit IDs that the current user can access (their unit + all sub-units in hierarchy)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unit IDs the user can access</returns>
    Task<IEnumerable<Guid>> GetAccessibleUnitIdsAsync(CancellationToken cancellationToken = default);
}
