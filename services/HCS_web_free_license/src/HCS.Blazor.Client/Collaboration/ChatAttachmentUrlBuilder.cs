using System;
using Microsoft.Extensions.Configuration;
using HCS.Blazor.Client.Navigation;

namespace HCS.Blazor.Client.Collaboration;

internal static class ChatAttachmentUrlBuilder
{
    public static string Build(IConfiguration configuration, Guid attachmentId)
        => GatewayResourceUrlBuilder.Build(configuration, $"api/chat/attachments/{attachmentId:D}");
}
