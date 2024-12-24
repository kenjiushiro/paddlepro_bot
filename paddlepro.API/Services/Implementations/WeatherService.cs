using System.Text.Json;
using Microsoft.Extensions.Options;
using paddlepro.API.Configurations;
using paddlepro.API.Models;
using paddlepro.API.Services.Interfaces;

namespace paddlepro.API.Services.Implementations;

public class WeatherService : IWeatherService
{
  private readonly ILogger<WeatherService> _logger;
  private readonly HttpClient _httpClient;
  private readonly WeatherServiceConfiguration _weatherConfig;

  public WeatherService(
      ILogger<WeatherService> logger,
      IOptions<WeatherServiceConfiguration> weatherConfig,
      HttpClient httpClient
      )
  {
    _logger = logger;
    _httpClient = httpClient;
    _weatherConfig = weatherConfig.Value;
  }

  public async Task<WeatherApiResponse> GetWeatherForecast()
  {
    var response = await _httpClient.GetAsync("");
    string responseBody = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<WeatherApiResponse>(
        responseBody,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
  }
}
