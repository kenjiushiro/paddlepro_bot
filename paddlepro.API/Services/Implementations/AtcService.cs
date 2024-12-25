using paddlepro.API.Configurations;
using paddlepro.API.Helpers;
using paddlepro.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;
using paddlepro.API.Models.Infrastructure;

namespace paddlepro.API.Services.Implementations;

public class AtcService : IPaddleService
{
    private readonly ILogger<AtcService> logger;
    private readonly HttpClient httpClient;
    private readonly AtcServiceConfiguration config;

    public AtcService(
        ILogger<AtcService> logger,
        HttpClient client,
        IOptions<AtcServiceConfiguration> config
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
        var queryParams = new Dictionary<string, string>
        {
          { "day", day },
          { "court", courtId },
          { "sport_id", config.SportId },
          { "duration", day },
          { "start", start },
        };
        return $"{baseUrl}{checkoutPath}/{clubId}{queryParams.ToQueryParams()}";
    }

    public async Task<AtcResponse> GetAvailability(string date)
    {
        var queryParams = new Dictionary<string, string>
        {
          { "horario", "19%3A30" },
          { "tipoDeporte", config.SportId },
          { "dia", date },
          { "placeId", config.PlaceId }
        };
        var listQuery = this.config.ListPath + queryParams.ToQueryParams();
        this.logger.LogInformation("URL: {Url}", listQuery);
        var response = await httpClient.GetAsync(listQuery);
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<AtcResponse>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
    }
}
