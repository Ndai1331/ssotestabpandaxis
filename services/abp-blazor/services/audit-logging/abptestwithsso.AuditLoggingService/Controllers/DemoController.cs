using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace abptestwithsso.AuditLoggingService.Controllers;

[Route("api/audit-logging/demo")]
[Area("audit-logging")]
[RemoteService(Name = "AuditLoggingService")]
public class DemoController : AbpController
{
    private readonly abptestwithssoMetrics _abptestwithssoMetrics;

    public DemoController(abptestwithssoMetrics abptestwithssoMetrics)
    {
        _abptestwithssoMetrics = abptestwithssoMetrics;
    }
    
    [HttpGet]
    [Route("hello")]
    public async Task<string> HelloWorld()
    {
        _abptestwithssoMetrics.IncrementHelloCounter();
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