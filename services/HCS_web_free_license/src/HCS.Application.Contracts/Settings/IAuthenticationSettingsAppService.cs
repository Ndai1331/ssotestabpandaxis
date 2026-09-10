using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace HCS.Settings;

public interface IAuthenticationSettingsAppService : IApplicationService
{
    Task<AuthenticationSettingsDto> GetAsync();
    Task UpdateAsync(UpdateAuthenticationSettingsDto input);
}
