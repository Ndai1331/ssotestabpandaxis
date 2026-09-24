using HCS.Settings;
using Shouldly;
using Xunit;

namespace HCS.SettingsTests;

public class ChatAttachmentSettingsTests
{
    [Fact]
    public void Parse_clamps_chat_attachment_megabytes_to_the_admin_range()
    {
        HCSSettings.ParseChatAttachmentMaxMegabytes(null).ShouldBe(HCSSettings.ChatAttachmentMaxMegabytesDefault);
        HCSSettings.ParseChatAttachmentMaxMegabytes("0").ShouldBe(HCSSettings.ChatAttachmentMaxMegabytesMin);
        HCSSettings.ParseChatAttachmentMaxMegabytes("25").ShouldBe(25);
        HCSSettings.ParseChatAttachmentMaxMegabytes("9000").ShouldBe(HCSSettings.ChatAttachmentMaxMegabytesMax);
        HCSSettings.ChatAttachmentMaxBytes(1).ShouldBe(1024 * 1024);
    }
}
