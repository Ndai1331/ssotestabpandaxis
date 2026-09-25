using System.Threading;
using System.Threading.Tasks;

namespace HCS.Coding;

public interface IAutoCodeSettings
{
    Task<string> GetPrefixAsync(AutoCodeKind kind, CancellationToken cancellationToken = default);
}

public sealed class DefaultAutoCodeSettings : IAutoCodeSettings
{
    public static DefaultAutoCodeSettings Instance { get; } = new();

    public Task<string> GetPrefixAsync(AutoCodeKind kind, CancellationToken cancellationToken = default) =>
        Task.FromResult(AutoCode.DefaultPrefix(kind));
}
