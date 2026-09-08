using System;
using System.Globalization;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace HCS.Localization;

public class Language : FullAuditedAggregateRoot<Guid>
{
    public string CultureName { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public bool IsEnabled { get; private set; }
    public bool IsDefault { get; private set; }

    protected Language()
    {
    }

    public Language(Guid id, string cultureName, string displayName, bool isEnabled = true, bool isDefault = false)
        : base(id)
    {
        SetCultureName(cultureName);
        SetDisplayName(displayName);
        IsEnabled = isEnabled;
        SetDefault(isDefault);
    }

    public void Update(string displayName, bool isEnabled)
    {
        if (IsDefault && !isEnabled)
        {
            throw new BusinessException(HCSDomainErrorCodes.DefaultLanguageRequired);
        }

        SetDisplayName(displayName);
        IsEnabled = isEnabled;
    }

    public void SetDefault(bool isDefault)
    {
        if (isDefault && !IsEnabled)
        {
            throw new BusinessException(HCSDomainErrorCodes.DefaultLanguageMustBeEnabled);
        }

        IsDefault = isDefault;
    }

    public static string NormalizeCultureName(string cultureName)
    {
        cultureName = Check.NotNullOrWhiteSpace(cultureName, nameof(cultureName), LanguageConsts.MaxCultureNameLength);
        try
        {
            return CultureInfo.GetCultureInfo(cultureName).Name;
        }
        catch (CultureNotFoundException)
        {
            throw new BusinessException(HCSDomainErrorCodes.LanguageInvalidCulture)
                .WithData("CultureName", cultureName);
        }
    }

    private void SetCultureName(string cultureName)
    {
        CultureName = NormalizeCultureName(cultureName);
    }

    private void SetDisplayName(string displayName)
    {
        DisplayName = Check.NotNullOrWhiteSpace(displayName, nameof(displayName), LanguageConsts.MaxDisplayNameLength);
    }
}
