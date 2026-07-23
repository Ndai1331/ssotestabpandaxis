using System.Diagnostics.Metrics;
using Volo.Abp.DependencyInjection;

namespace abptestwithsso.LanguageService;

public class abptestwithssoMetrics : ISingletonDependency
{
    public const string MeterName = "abptestwithsso.Api";

    private readonly Counter<long> _helloRequestCounter;

    public abptestwithssoMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _helloRequestCounter = meter.CreateCounter<long>("hello_requests.count");
    }

    public void IncrementHelloCounter()
    {
        _helloRequestCounter.Add(1);
    }
}