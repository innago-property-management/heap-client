namespace Innago.Shared.HeapService;

using Prometheus;

internal static class MyMetrics
{
    public static readonly Counter MyCounter = Metrics.CreateCounter("my_counter", "description", "error_code");
}