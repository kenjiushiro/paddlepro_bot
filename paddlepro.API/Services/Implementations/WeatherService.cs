using System.Text.Json;
using Microsoft.Extensions.Options;
using paddlepro.API.Configurations;
using paddlepro.API.Helpers;
using paddlepro.API.Models.Infrastructure;
using paddlepro.API.Services.Interfaces;

namespace paddlepro.API.Services.Implementations;

public class WeatherService : IWeatherService
{
  private readonly ILogger<WeatherService> logger;
  private readonly HttpClient httpClient;
  private readonly WeatherServiceConfiguration weatherConfig;

  public WeatherService(
      ILogger<WeatherService> logger,
      IOptions<WeatherServiceConfiguration> weatherConfig,
      HttpClient httpClient
      )
  {
    this.logger = logger;
    this.httpClient = httpClient;
    this.weatherConfig = weatherConfig.Value;
  }

  public async Task<ForecastDay[]> GetWeatherForecast(string city = "Buenos%20Aires")
  {
    var queryParams = new Dictionary<string, string> {
      {"q", city},
      {"days", this.weatherConfig.DaysInAdvance.ToString()},
      {"key", this.weatherConfig.ApiKey}
    };

    var response = await this.httpClient.GetAsync($"/v1/forecast.json{queryParams.ToQueryParams()}");
    response.EnsureSuccessStatusCode();

    this.logger.LogInformation("Fetching weather");
    string responseBody = await response.Content.ReadAsStringAsync();

    var weatherResponse = JsonSerializer.Deserialize<WeatherApiResponse>(
        responseBody,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    return weatherResponse.Forecast.Forecastday;
  }
}
