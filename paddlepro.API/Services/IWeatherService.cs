using paddlepro.API.Models;

namespace paddlepro.API.Services;

public interface IWeatherService
{
  WeatherForecast GetWeatherForecast(string date);
}
