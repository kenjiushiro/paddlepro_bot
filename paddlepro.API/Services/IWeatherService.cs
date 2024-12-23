using paddlepro.API.Models;

namespace paddlepro.API.Services;

public interface IWeatherService
{
    Task<WeatherApiResponse> GetWeatherForecast();
}
