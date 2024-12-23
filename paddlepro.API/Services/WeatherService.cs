using System.Text.Json;
using Microsoft.Extensions.Options;
using paddlepro.API.Configurations;
using paddlepro.API.Models;

namespace paddlepro.API.Services;

public class WeatherService : IWeatherService
{
    public ILogger<WeatherService> _logger;
    public HttpClient _httpClient;
    public WeatherServiceConfiguration _weatherConfig;

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
