using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OtelCollection.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController(ILogger<WeatherForecastController> logger) : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing",
            "Bracing",
            "Chilly",
            "Cool",
            "Mild",
            "Warm",
            "Balmy",
            "Hot",
            "Sweltering",
            "Scorching"
        ];

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get([FromServices] ActivitySource forecastActivitySource, [FromServices] Counter<int> forecastCounter)
        {
            var forecast = Enumerable
                .Range(1, 5)
                .Select(index => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();

            // Log a message
            logger.LogInformation("Date: {Date}, Temperature: {Temperature}", forecast[0].Date, forecast[0].TemperatureC);

            using var forecastActivity = forecastActivitySource.StartActivity("GetWeatherForecast");

            // Increment the custom counter
            forecastCounter.Add(1);

            // Add a tag to the activity
            forecastActivity!.SetTag("Temperature", forecast[0].TemperatureC);
            forecastActivity!.SetTag("Summary", forecast[0].Summary);

            return forecast;
        }
    }
}
