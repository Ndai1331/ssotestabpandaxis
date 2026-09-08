using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Pages;
using HCS.Localization;

namespace HCS.Blazor.Client.Layouts;

public sealed class LanguageCatalogState(LanguageManagementClient client)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IReadOnlyList<LanguageOptionDto> languages = [];
    private bool loaded;

    public event Action? Changed;

    public IReadOnlyList<LanguageOptionDto> Languages => languages;

    public string DefaultCultureName => languages.FirstOrDefault(x => x.IsDefault)?.CultureName
        ?? languages.FirstOrDefault()?.CultureName
        ?? "en";

    public async Task<IReadOnlyList<LanguageOptionDto>> EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (loaded) return languages;

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!loaded)
            {
                languages = await client.GetEnabledLanguagesAsync(cancellationToken);
                loaded = true;
            }

            return languages;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            languages = await client.GetEnabledLanguagesAsync(cancellationToken);
            loaded = true;
        }
        finally
        {
            gate.Release();
        }

        Changed?.Invoke();
    }
}
