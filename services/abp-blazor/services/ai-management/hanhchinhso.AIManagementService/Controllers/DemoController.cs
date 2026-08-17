using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace hanhchinhso.AIManagementService.Controllers;

[Route("api/ai-management/demo")]
[Area("ai-management")]
[RemoteService(Name = "AIManagementService")]
public class DemoController : AbpController
{
    private readonly hanhchinhsoMetrics _hanhchinhsoMetrics;

    public DemoController(hanhchinhsoMetrics hanhchinhsoMetrics)
    {
        _hanhchinhsoMetrics = hanhchinhsoMetrics;
    }
    
    [HttpGet]
    [Route("hello")]
    public async Task<string> HelloWorld()
    {
        _hanhchinhsoMetrics.IncrementHelloCounter();
        return await Task.FromResult("Hello World!");
    }
    
    [HttpGet]
    [Route("hello-authorized")]
    [Authorize]
    public async Task<string> HelloWorldAuthorized()
    {
        return await Task.FromResult("Hello World (Authorized)!");
    }
}
