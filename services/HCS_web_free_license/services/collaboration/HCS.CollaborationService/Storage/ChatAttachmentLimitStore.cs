using HCS.CollaborationService.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Volo.Abp.DependencyInjection;

namespace HCS.CollaborationService.Storage;

public sealed class ChatAttachmentLimitStore(IConfiguration configuration, IServiceProvider services)
    : ISingletonDependency
{
    public const string RedisKey = "hcs:chat:attachment-max-mb";

    private int? memory;

    public int GetMaxMegabytes()
    {
        if (TryReadRedis(out var redisValue))
        {
            return ChatAttachmentPolicy.ClampMegabytes(redisValue);
        }

        if (memory is int cached)
        {
            return ChatAttachmentPolicy.ClampMegabytes(cached);
        }

        var configured = configuration.GetValue<int?>("AttachmentPolicy:MaxMegabytes");
        return configured is int megabytes
            ? ChatAttachmentPolicy.ClampMegabytes(megabytes)
            : ChatAttachmentPolicy.DefaultMegabytes;
    }

    public long GetMaxBytes() => ChatAttachmentPolicy.ToBytes(GetMaxMegabytes());

    public void SetMaxMegabytes(int megabytes)
    {
        var clamped = ChatAttachmentPolicy.ClampMegabytes(megabytes);
        memory = clamped;
        services.GetService<IConnectionMultiplexer>()?.GetDatabase()
            .StringSet(RedisKey, clamped.ToString());
    }

    private bool TryReadRedis(out int megabytes)
    {
        megabytes = 0;
        var redis = services.GetService<IConnectionMultiplexer>();
        if (redis is null)
        {
            return false;
        }

        var raw = redis.GetDatabase().StringGet(RedisKey);
        return raw.HasValue && int.TryParse(raw.ToString(), out megabytes);
    }
}
