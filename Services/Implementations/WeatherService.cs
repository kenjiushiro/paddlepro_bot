using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using paddlepro.API.Configurations;
using paddlepro.API.Helpers;
using paddlepro.API.Models.Application;
using paddlepro.API.Models.Infrastructure;
using paddlepro.API.Services.Interfaces;

namespace paddlepro.API.Services.Implementations;

public class WeatherService : IWeatherService
{
  private readonly ILogger<WeatherService> logger;
  private readonly HttpClient httpClient;
  private readonly IMapper mapper;
  private readonly IMemoryCache cache;
  private readonly WeatherServiceConfiguration weatherConfig;
  private readonly string cacheKey = "weather";

  public WeatherService(
      ILogger<WeatherService> logger,
      IOptions<WeatherServiceConfiguration> weatherConfig,
      IMemoryCache cache,
      IMapper mapper,
      HttpClient httpClient
      )
  {
    this.logger = logger;
    this.cache = cache;
    this.mapper = mapper;
    this.httpClient = httpClient;
    this.weatherConfig = weatherConfig.Value;
  }

  private async Task<WeatherForecast[]> DoGetWeatherForecast(string city = "Buenos%20Aires")
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

    return this.mapper.Map<WeatherForecast[]>(weatherResponse?.Forecast.Forecastday);
  }

  public async Task<WeatherForecast[]> GetWeatherForecast(string city = "Buenos%20Aires")
  {
    if (!this.cache.TryGetValue<WeatherForecast[]>(this.cacheKey, out WeatherForecast[] forecast))
    {
      (int hours, int minutes, int seconds) = (1, 0, 0);
      TimeSpan expiration = new TimeSpan(hours, minutes, seconds);
      forecast = this.cache.Set(
          this.cacheKey,
          await this.DoGetWeatherForecast(city),
          new MemoryCacheEntryOptions
          {
            AbsoluteExpirationRelativeToNow = expiration
          });
    }
    return forecast!;
  }
}
