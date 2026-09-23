namespace HCS.OrganizationService.Application;

public interface IUnitDepartmentLookup
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
