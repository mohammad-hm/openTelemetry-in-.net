using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace otlSample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
    };

        private static readonly HttpClient HttpClient = new();

        private readonly ILogger<WeatherForecastController> logger;
        private readonly ActivitySource activitySource;
        private readonly Counter<long> freezingDaysCounter;
        private readonly Counter<long> counterOfController;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, Instrumentation instrumentation)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ArgumentNullException.ThrowIfNull(instrumentation);
            this.activitySource = instrumentation.ActivitySource;
            this.freezingDaysCounter = instrumentation.FreezingDaysCounter;
            this.counterOfController = instrumentation.CountOfCallingController;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            using var scope = this.logger.BeginScope("{Id}", Guid.NewGuid().ToString("N"));

           
            using var activity = this.activitySource.StartActivity("parent activity");

            var rng = new Random();
            var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)],
            })
            .ToArray();

            // Optional: Count the freezing days
            this.freezingDaysCounter.Add(forecast.Count(f => f.TemperatureC < 0));

            // Optional: Count the call controller
            this.counterOfController.Add(1);

            this.logger.LogInformation(
                "WeatherForecasts generated {count}: {forecasts}",
                forecast.Length,
                forecast);

            // Add a tag to the Activity. let you attach key/value pairs to an Activity so it carries more information about the current operation that it’s tracking.
            activity?.SetTag("greeting", "Hello World!");

        


            using (var child1 = this.activitySource.StartActivity("child1"))
            {
                Thread.Sleep(new TimeSpan(0, 0, 5));

            }

            using (var child2 = this.activitySource.StartActivity("child2"))
            {

                Thread.Sleep(new TimeSpan(0, 0, 1));
                // Do some work that 'child2' tracks
            }

            return forecast;
        }

    }
}