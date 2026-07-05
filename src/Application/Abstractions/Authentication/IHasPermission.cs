namespace Application.Abstractions.Authentication;

public interface IHasPermission
{


    /// <summary>
    /// Gets all organizational unit IDs that the current user can access (their unit + all sub-units in hierarchy)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unit IDs the user can access</returns>
    Task<IEnumerable<Guid>> GetAccessibleUnitIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the current user may perform write operations on the given employee.
    /// Admin/SuperAdmin can manage anyone; other roles are restricted to employees
    /// whose organizational unit falls within their accessible unit tree.
    /// </summary>
    /// <param name="employeeId">The employee being written to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the write is allowed</returns>
    Task<bool> CanManageEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
