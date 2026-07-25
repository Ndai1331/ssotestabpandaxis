using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Uow;

namespace hanhchinhso.DocumentService.Signing;

public interface ISigningMutationCoordinator
{
    Task<TResult> ExecuteAsync<TResult>(
        Guid? tenantId,
        string providerCode,
        Func<Task<TResult>> action);
}

public static class SigningMutationLock
{
    public static string GetName(Guid? tenantId)
    {
        var tenantKey = tenantId?.ToString("N") ?? "host";
        return $"document-signing-metadata:{tenantKey}";
    }
}

public sealed class SigningMutationCoordinator :
    ISigningMutationCoordinator,
    ITransientDependency
{
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public SigningMutationCoordinator(
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _distributedLock = distributedLock;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Guid? tenantId,
        string providerCode,
        Func<Task<TResult>> action)
    {
        await using var handle = await _distributedLock.TryAcquireAsync(
            SigningMutationLock.GetName(tenantId),
            TimeSpan.FromSeconds(30));
        if (handle is null)
        {
            throw new UserFriendlyException(
                "The signing provider is busy. Please retry.");
        }

        using var uow = _unitOfWorkManager.Begin(
            requiresNew: true,
            isTransactional: true);
        var result = await action();
        await uow.CompleteAsync();
        return result;
    }
}
