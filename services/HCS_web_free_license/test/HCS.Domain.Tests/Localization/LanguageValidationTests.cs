using System;
using HCS.Localization;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace HCS.LocalizationTests;

public class LanguageValidationTests
{
    [Fact]
    public void Should_Reject_Empty_Culture_Name()
    {
        Should.Throw<ArgumentException>(() => new Language(Guid.NewGuid(), " ", "Invalid"));
    }

    [Fact]
    public void Should_Normalize_Culture_Name_Using_CultureInfo()
    {
        var language = new Language(Guid.NewGuid(), "fr-fr", "Français");

        language.CultureName.ShouldBe("fr-FR");
    }

    [Fact]
    public void Should_Reject_Invalid_Culture_Name()
    {
        var exception = Should.Throw<BusinessException>(() => new Language(Guid.NewGuid(), "!!!", "Invalid"));

        exception.Code.ShouldBe(HCSDomainErrorCodes.LanguageInvalidCulture);
    }

    [Fact]
    public void Should_Reject_Disabled_Default_Language()
    {
        var exception = Should.Throw<BusinessException>(() => new Language(
            Guid.NewGuid(), "fr-FR", "Français", isEnabled: false, isDefault: true));

        exception.Code.ShouldBe(HCSDomainErrorCodes.DefaultLanguageMustBeEnabled);
    }

    [Fact]
    public void Should_Reject_Overlong_Translation()
    {
        Should.Throw<ArgumentException>(() => new LanguageText(
            Guid.NewGuid(), "HCS", "en", "Greeting", new string('x', LanguageConsts.MaxTextValueLength + 1)));
    }
}
