using System.Text.Json;
using Microsoft.Extensions.Options;
using paddlepro.API.Configurations;
using paddlepro.API.Models.Infrastructure;
using paddlepro.API.Models.Application;
using paddlepro.API.Services.Interfaces;
using AutoMapper;

namespace paddlepro.API.Services.Implementations;

public class WeatherService : IWeatherService
{
  private readonly ILogger<WeatherService> _logger;
  private readonly HttpClient _httpClient;
  private readonly WeatherServiceConfiguration _weatherConfig;
  private readonly IMapper _mapper;

  public WeatherService(
      ILogger<WeatherService> logger,
      IOptions<WeatherServiceConfiguration> weatherConfig,
      HttpClient httpClient,
      IMapper mapper
      )
  {
    _logger = logger;
    _httpClient = httpClient;
    _weatherConfig = weatherConfig.Value;
    _mapper = mapper;
  }

  public async Task<Models.Application.WeatherForecast[]> GetWeatherForecast()
  {
    var response = await _httpClient.GetAsync("");
    string responseBody = await response.Content.ReadAsStringAsync();

    var weatherResponse = JsonSerializer.Deserialize<WeatherApiResponse>(
        responseBody,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    return _mapper.Map<Models.Application.WeatherForecast[]>(weatherResponse.Forecast.Forecastday);
  }
}
