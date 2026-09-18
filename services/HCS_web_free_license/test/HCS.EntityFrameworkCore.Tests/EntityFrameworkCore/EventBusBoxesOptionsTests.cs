using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace HCS.EntityFrameworkCore;

[Collection(HCSTestConsts.CollectionDefinitionName)]
public class EventBusBoxesOptionsTests : HCSEntityFrameworkCoreTestBase
{
    [Fact]
    public void Should_Retry_Failed_Inbox_Events_Later_Then_Discard()
    {
        var options = GetRequiredService<IOptions<AbpEventBusBoxesOptions>>().Value;

        options.InboxProcessorFailurePolicy.ShouldBe(InboxProcessorFailurePolicy.RetryLater);
        options.InboxProcessorMaxRetryCount.ShouldBe(10);
    }
}
