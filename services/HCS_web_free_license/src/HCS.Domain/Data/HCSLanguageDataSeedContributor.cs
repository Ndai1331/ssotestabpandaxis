using System.Threading.Tasks;
using HCS.Localization;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace HCS.Data;

public sealed class HCSLanguageDataSeedContributor(
    ILanguageRepository languageRepository,
    IGuidGenerator guidGenerator) : IDataSeedContributor, ITransientDependency
{
    public async Task SeedAsync(DataSeedContext context)
    {
        if (await languageRepository.GetCountAsync() > 0)
        {
            return;
        }

        await languageRepository.InsertAsync(
            new Language(guidGenerator.Create(), "vi", "Tiếng Việt", isEnabled: true, isDefault: true),
            autoSave: false);
        await languageRepository.InsertAsync(
            new Language(guidGenerator.Create(), "en", "English", isEnabled: true),
            autoSave: true);
    }
}
