using System;
using HCS.Blazor.Client.Services;
using HCS.Localization;
using Volo.Abp.AspNetCore.Components;

namespace HCS.Blazor.Client;

public abstract class HCSComponentBase : AbpComponentBase
{
    protected HCSComponentBase()
    {
        LocalizationResource = typeof(HCSResource);
    }

    protected string MapBffError(Exception exception, BffErrorKind kind = BffErrorKind.Load) =>
        BffErrorMapper.From(L, exception, kind);
}
