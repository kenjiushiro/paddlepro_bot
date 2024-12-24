using paddlepro.API.Models;

namespace paddlepro.API.Services.Interfaces;

public interface IWeatherService
{
  Task<WeatherApiResponse> GetWeatherForecast();
}
