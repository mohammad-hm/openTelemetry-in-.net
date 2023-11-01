namespace otlSample;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// It is recommended to use a custom type to hold references for
/// ActivitySource and Instruments. This avoids possible type collisions
/// with other components in the DI container.
/// </summary>
public class Instrumentation : IDisposable
{
    internal const string ActivitySourceName = "Example.Trace.AspNetCore";
    internal const string MeterName = "Example.Meter.AspNetCore";
    private readonly Meter meter;


    public Instrumentation()
    {

        // Declare the boundaries for the histogram
        var boundaries = new List<double> { 0.1, 0.5, 1.0, 5.0, 10.0 };
        string? version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();
        this.ActivitySource = new ActivitySource(ActivitySourceName, version);
        this.meter = new Meter(MeterName, version);
        this.CountOfCallingController = this.meter.CreateCounter<long>("count.wetherforecast", "The number of calling the wetherforecast controller");
        this.FreezingDaysCounter = this.meter.CreateCounter<long>("weather.days.freezing", "The number of days where the temperature is below freezing");
        this.durationOfResponding = this.meter.CreateHistogram<long>("histogram.wetherforecast", "monitor the distribution of response times.");
    }

    public ActivitySource ActivitySource { get; }

    public Counter<long> FreezingDaysCounter { get; }

    public Counter<long> CountOfCallingController { get; }

    public Histogram<long> durationOfResponding { get; }

    public void Dispose()
    {
        this.ActivitySource.Dispose();
        this.meter.Dispose();
    }
}
