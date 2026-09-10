using System.Threading.Tasks;
using HCS.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCS.Controllers.Settings;

[Authorize(Roles = "admin")]
[Route("api/hcs/authentication-settings")]
public sealed class AuthenticationSettingsController : HCSController, IAuthenticationSettingsAppService
{
    private readonly IAuthenticationSettingsAppService _service;

    public AuthenticationSettingsController(IAuthenticationSettingsAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<AuthenticationSettingsDto> GetAsync() => _service.GetAsync();

    [HttpPut]
    public Task UpdateAsync(UpdateAuthenticationSettingsDto input) => _service.UpdateAsync(input);
}
