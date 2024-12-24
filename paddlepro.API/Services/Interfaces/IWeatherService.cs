using paddlepro.API.Models.Infrastructure;

namespace paddlepro.API.Services.Interfaces;

public interface IWeatherService
{
  Task<ForecastDay[]> GetWeatherForecast();
}
