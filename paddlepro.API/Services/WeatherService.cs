using paddlepro.API.Models;

namespace paddlepro.API.Services;

public class WeatherService : IWeatherService
{
    public ILogger<WeatherService> _logger;

    public WeatherService(
        ILogger<WeatherService> logger
        )
    {
        _logger = logger;
    }


    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };


    public WeatherForecast GetWeatherForecast(DateTime date)
    {
        _logger.LogInformation(date.ToString());
        return new WeatherForecast
        {
            Date = DateOnly.FromDateTime(date),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        };
    }
}
