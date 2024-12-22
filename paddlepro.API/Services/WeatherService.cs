using paddlepro.API.Configurations;
using paddlepro.API.Models;

namespace paddlepro.API.Services;

public class WeatherService : IWeatherService
{
    public ILogger<WeatherService> _logger;
    public IHttpClientFactory _httpClientFactory;
    /*public WeatherServiceConfiguration _config;*/

    public WeatherService(
        /*WeatherServiceConfiguration config,*/
        ILogger<WeatherService> logger,
        IHttpClientFactory httpClientFactory
        )
    {
        /*_config = config;*/
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        /*Console.WriteLine("BASE URL: ", _config.BaseUrl);*/
    }

    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };


    public WeatherForecast GetWeatherForecast(DateTime date)
    {
        _logger.LogInformation(date.ToString());
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(date),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).First();
    }
}
