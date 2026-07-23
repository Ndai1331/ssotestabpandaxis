using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace abptestwithsso.IdentityService.Controllers;

[Route("api/identity/demo")]
[Area("identity")]
[RemoteService(Name = "IdentityService")]
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
}