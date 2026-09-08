using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace HCS.Localization;

public class LanguageManager : DomainService
{
    private readonly ILanguageRepository _languageRepository;

    public LanguageManager(ILanguageRepository languageRepository)
    {
        _languageRepository = languageRepository;
    }

    public async Task<Language> CreateAsync(string cultureName, string displayName, bool isEnabled, bool isDefault)
    {
        var normalizedCultureName = Language.NormalizeCultureName(cultureName);
        if (isDefault && !isEnabled)
        {
            throw new BusinessException(HCSDomainErrorCodes.DefaultLanguageMustBeEnabled);
        }

        if (await _languageRepository.FindByCultureNameAsync(normalizedCultureName) != null)
        {
            throw new BusinessException(HCSDomainErrorCodes.LanguageAlreadyExists).WithData("CultureName", normalizedCultureName);
        }

        var language = new Language(GuidGenerator.Create(), normalizedCultureName, displayName, isEnabled, isDefault);
        if (isDefault)
        {
            await ClearCurrentDefaultAsync();
        }

        return language;
    }

    public async Task SetDefaultAsync(Language language)
    {
        if (!language.IsEnabled)
        {
            throw new BusinessException(HCSDomainErrorCodes.DefaultLanguageMustBeEnabled);
        }

        await ClearCurrentDefaultAsync(language.Id);
        language.SetDefault(true);
    }

    public async Task EnsureCanDeleteAsync(Language language)
    {
        if (language.IsDefault)
        {
            throw new BusinessException(HCSDomainErrorCodes.DefaultLanguageRequired);
        }

        await Task.CompletedTask;
    }

    private async Task ClearCurrentDefaultAsync(Guid? exceptId = null)
    {
        var current = await _languageRepository.FindDefaultAsync();
        if (current != null && current.Id != exceptId)
        {
            current.SetDefault(false);
            await _languageRepository.UpdateAsync(current, autoSave: false);
        }
    }
}
