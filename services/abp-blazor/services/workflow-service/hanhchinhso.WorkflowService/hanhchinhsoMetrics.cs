using System.Diagnostics.Metrics;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.WorkflowService;

public class hanhchinhsoMetrics : ISingletonDependency
{
    public const string MeterName = "hanhchinhso.Api";

    private readonly Counter<long> _helloRequestCounter;

    public hanhchinhsoMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _helloRequestCounter = meter.CreateCounter<long>("hello_requests.count");
    }

    public void IncrementHelloCounter()
    {
        _helloRequestCounter.Add(1);
    }
}