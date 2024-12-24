using paddlepro.API.Models;
using paddlepro.API.Configurations;
using paddlepro.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;
using paddlepro.API.Models.Infrastructure;

namespace paddlepro.API.Services.Implementations;

public class AtcService : IPaddleService
{
  private readonly ILogger<AtcService> _logger;
  private readonly HttpClient httpClient;
  private readonly PaddleServiceConfiguration _config;

  public AtcService(
      ILogger<AtcService> logger,
      HttpClient client,
      IOptions<PaddleServiceConfiguration> config
    )
  {
    _logger = logger;
    httpClient = client;
    _config = config.Value;
  }

  public async Task<Club[]> GetAvailabilities(string date)
  {
    var queryParams = "?horario=19%3A30&tipoDeporte=7&dia=2024-12-24&placeId=69y7pkxfg";
    var response = await httpClient.GetAsync(queryParams);

    string responseBody = await response.Content.ReadAsStringAsync();

    var atcResponse = JsonSerializer.Deserialize<AtcResponse>(
        responseBody,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    _logger.LogInformation("Response?: {Response}", atcResponse.PageProps.BookingsBySport.Length);

    return Array.Empty<Club>();
  }
}
