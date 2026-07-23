using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace abptestwithsso.AdministrationService.Controllers;

[Route("api/administration/demo")]
[Area("administration")]
[RemoteService(Name = "AdministrationService")]
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