using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;

namespace hanhchinhso.DocumentService.Signing;

public interface ISigningAssetLock
{
    Task<TResult> ExecuteAsync<TResult>(
        Guid? tenantId,
        IEnumerable<Guid> assetIds,
        Func<Task<TResult>> action);
}

public sealed class SigningAssetLock :
    ISigningAssetLock,
    ITransientDependency
{
    private readonly IAbpDistributedLock _distributedLock;

    public SigningAssetLock(IAbpDistributedLock distributedLock) =>
        _distributedLock = distributedLock;

    public async Task<TResult> ExecuteAsync<TResult>(
        Guid? tenantId,
        IEnumerable<Guid> assetIds,
        Func<Task<TResult>> action)
    {
        var tenantKey = tenantId?.ToString("N") ?? "host";
        var handles = new List<IAbpDistributedLockHandle>();
        try
        {
            foreach (var id in assetIds
                         .Where(x => x != Guid.Empty)
                         .Distinct()
                         .Order())
            {
                var handle = await _distributedLock.TryAcquireAsync(
                    $"document-signing-asset:{tenantKey}:{id:N}",
                    TimeSpan.FromSeconds(30));
                if (handle is null)
                {
                    throw new UserFriendlyException(
                        "A signing asset is busy. Please retry.");
                }
                handles.Add(handle);
            }
            return await action();
        }
        finally
        {
            for (var index = handles.Count - 1; index >= 0; index--)
            {
                await handles[index].DisposeAsync();
            }
        }
    }
}
