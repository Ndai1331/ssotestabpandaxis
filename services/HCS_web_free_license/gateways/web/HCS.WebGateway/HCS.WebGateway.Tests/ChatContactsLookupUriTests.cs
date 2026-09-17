using HCS.Blazor.Client.Collaboration;
using HCS.CollaborationService.Contracts;
using Xunit;

namespace HCS.WebGateway.Tests;

public sealed class ChatContactsLookupUriTests
{
    [Fact]
    public void Builds_chat_contact_lookup_without_identity_admin_route()
    {
        var first = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var second = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var actual = CollaborationClient.BuildContactsLookupUri([Guid.Empty, first, first, second]);

        Assert.Equal(
            "api/chat/contacts/lookup?userIds=11111111-1111-1111-1111-111111111111&userIds=22222222-2222-2222-2222-222222222222",
            actual);
        Assert.DoesNotContain("identity/users", actual, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Caps_lookup_ids_to_the_shared_contract_limit()
    {
        var ids = Enumerable.Range(1, ChatContactLookup.MaxIds + 10)
            .Select(index => Guid.Parse($"00000000-0000-0000-0000-{index:D12}"));

        var actual = CollaborationClient.BuildContactsLookupUri(ids);

        Assert.Equal(ChatContactLookup.MaxIds, actual.Split("userIds=", StringSplitOptions.None).Length - 1);
    }
}
