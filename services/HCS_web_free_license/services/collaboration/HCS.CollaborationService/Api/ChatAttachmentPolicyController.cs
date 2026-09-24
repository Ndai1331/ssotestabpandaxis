using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace HCS.CollaborationService.Api;

[ApiController, Authorize, Route("api/chat")]
public sealed class ChatAttachmentPolicyController(ChatAttachmentLimitStore limits) : AbpControllerBase
{
    [HttpGet("attachment-policy")]
    public ChatAttachmentPolicyDto GetAttachmentPolicy() => new(limits.GetMaxMegabytes());

    [HttpPut("attachment-policy")]
    public ActionResult<ChatAttachmentPolicyDto> SetAttachmentPolicy([FromBody] ChatAttachmentPolicyDto input)
    {
        if (!User.HasClaim("permission", "HCS.SystemBranding.Update")
            && !User.HasClaim("permission", CollaborationPermissions.Administration))
        {
            return Forbid();
        }

        limits.SetMaxMegabytes(input.MaxMegabytes);
        return Ok(new ChatAttachmentPolicyDto(limits.GetMaxMegabytes()));
    }
}
