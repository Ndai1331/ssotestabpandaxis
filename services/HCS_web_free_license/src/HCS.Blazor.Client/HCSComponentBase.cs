using System;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using HCS.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Volo.Abp.AspNetCore.Components;
using Volo.Abp.AspNetCore.Components.Messages;

namespace HCS.Blazor.Client;

public abstract class HCSComponentBase : AbpComponentBase
{
    protected HCSComponentBase()
    {
        LocalizationResource = typeof(HCSResource);
    }

    [Inject] protected IUiMessageService UiMessageService { get; set; } = default!;

    protected string MapBffError(Exception exception, BffErrorKind kind = BffErrorKind.Load) =>
        BffErrorMapper.From(L, exception, kind);

    protected Task NotifySuccessAsync(string message) => UiMessageService.Success(message);

    protected Task NotifySuccessAsync(LocalizedString message) => UiMessageService.Success(message);

    protected Task NotifyErrorAsync(Exception exception, BffErrorKind kind = BffErrorKind.Load) =>
        UiMessageService.Error(MapBffError(exception, kind));

    protected Task NotifyErrorAsync(string message) => UiMessageService.Error(message);

    protected Task NotifyErrorAsync(LocalizedString message) => UiMessageService.Error(message);
}
