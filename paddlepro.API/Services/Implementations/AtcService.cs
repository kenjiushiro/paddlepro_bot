using paddlepro.API.Configurations;
using paddlepro.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;
using paddlepro.API.Models.Infrastructure;

namespace paddlepro.API.Services.Implementations;

public class AtcService : IPaddleService
{
  private readonly ILogger<AtcService> logger;
  private readonly HttpClient httpClient;
  private readonly PaddleServiceConfiguration config;

  public AtcService(
      ILogger<AtcService> logger,
      HttpClient client,
      IOptions<PaddleServiceConfiguration> config
    )
  {
    this.logger = logger;
    this.httpClient = client;
    this.config = config.Value;
  }

  public string GetCheckoutUrl(string clubId, string day, string courtId, string start, string duration)
  {
    string baseUrl = this.config.BaseUrl;
    string checkoutPath = this.config.CheckoutPath;
    return $"{baseUrl}{checkoutPath}/{clubId}?day={day}&court={courtId}&sport_id=7&duration={duration}&start={start}";
  }

  public async Task<AtcResponse> GetAvailability(string date)
  {
    var queryParams = $"?horario=19%3A30&tipoDeporte=7&dia={date}&placeId=69y7pkxfg";
    var response = await httpClient.GetAsync(this.config.ListPath + queryParams);

    string responseBody = await response.Content.ReadAsStringAsync();

    return JsonSerializer.Deserialize<AtcResponse>(
        responseBody,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
  }
}
