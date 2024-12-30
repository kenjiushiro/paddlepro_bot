using paddlepro.API.Models.Application;

namespace paddlepro.API.Services.Interfaces;

public interface IWeatherService
{
  Task<WeatherForecast[]> GetWeatherForecast(string city = "Buenos%20Aires");
}
