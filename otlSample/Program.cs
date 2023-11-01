using System.Diagnostics.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using otlSample;
using static System.Net.WebRequestMethods;

var appBuilder = WebApplication.CreateBuilder(args);

// Build a resource configuration action to set service information.
Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: appBuilder.Configuration.GetValue("ServiceName", defaultValue: "openTelemetry-hajimohammadi-service")!,
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");

// Create a service to expose ActivitySource, and Metric Instruments
// for manual instrumentation
appBuilder.Services.AddSingleton<Instrumentation>();

// Configure OpenTelemetry tracing & metrics with auto-start using the
// AddOpenTelemetry extension from OpenTelemetry.Extensions.Hosting.
appBuilder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithTracing(builder =>
    {
        // Tracing

        // Ensure the TracerProvider subscribes to any custom ActivitySources.
        builder
            .AddSource(Instrumentation.ActivitySourceName)
            .SetSampler(new AlwaysOnSampler())
        // collects metrics and traces about outgoing HTTP requests.
        // use this package for this:OpenTelemetry.Instrumentation.Http
        // the instrumentation emits this metric/trace:http.client.duration	that Measures the duration of outbound HTTP requests.
        .AddHttpClientInstrumentation()
        // collects metrics and traces about incoming  HTTP requests.
        // use this package for this:OpenTelemetry.Instrumentation.AspNetCore
        // the instrumentation emits this metric/trace:http.server.duration	that Measures the duration of inbound  HTTP requests.
        .AddAspNetCoreInstrumentation();

        // Use IConfiguration binding for AspNetCore instrumentation options.
        appBuilder.Services.Configure<AspNetCoreInstrumentationOptions>(appBuilder.Configuration.GetSection("AspNetCoreInstrumentation"));


        builder.AddJaegerExporter(o =>
         {
             o.Endpoint = new Uri("http://ip:4318");
             o.Protocol = JaegerExportProtocol.HttpBinaryThrift;
         });
        builder.AddOtlpExporter(otlpOptions =>
        {
            // Use IConfiguration directly for Otlp exporter endpoint option.
            otlpOptions.Endpoint = new Uri(appBuilder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://ip:4317")!);
            otlpOptions.Protocol = OtlpExportProtocol.Grpc;
        });

        builder.AddConsoleExporter();
    })
    .WithMetrics(builder =>
    {
        // Metrics

        // Ensure the MeterProvider subscribes to any custom Meters.
        builder
            .AddMeter(Instrumentation.MeterName)

        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation();

        builder.AddConsoleExporter();
        builder.AddOtlpExporter(otlpOptions =>
        {
            // Use IConfiguration directly for Otlp exporter endpoint option.
            otlpOptions.Endpoint = new Uri(appBuilder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://ip:4317")!);
            otlpOptions.Protocol = OtlpExportProtocol.Grpc;
        });
    });






// Add services to the container.

appBuilder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
appBuilder.Services.AddEndpointsApiExplorer();
appBuilder.Services.AddSwaggerGen();

var app = appBuilder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
