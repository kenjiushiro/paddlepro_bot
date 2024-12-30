using paddlepro.API.Configurations;
using paddlepro.API.Helpers;
using paddlepro.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;
using paddlepro.API.Models.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using paddlepro.API.Models.Application;
using AutoMapper;

namespace paddlepro.API.Services.Implementations;

public class AtcService : IPaddleService
{
  private readonly ILogger<AtcService> logger;
  private readonly HttpClient httpClient;
  private readonly AtcServiceConfiguration config;
  private readonly IMemoryCache cache;
  private readonly IMapper mapper;

  public AtcService(
      ILogger<AtcService> logger,
      HttpClient client,
      IOptions<AtcServiceConfiguration> config,
      IMemoryCache cache,
      IMapper mapper
    )
  {
    this.logger = logger;
    this.httpClient = client;
    this.config = config.Value;
    this.cache = cache;
    this.mapper = mapper;
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
          { "duration", duration },
          { "start", start },
        };
    return $"{baseUrl}{checkoutPath}/{clubId}{queryParams.ToQueryParams()}";
  }

  private async Task<Availability> DoGetAvailability(string date)
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

    var deserialized = JsonSerializer.Deserialize<AtcResponse>(
        responseBody,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    var availability = this.mapper.Map<Availability>(deserialized);
    this.cache.Set("availability", availability);
    return availability;
  }

  public Club GetClubDetails(string id)
  {
    var availability = this.cache.Get<Availability>("availability");
    return availability?.Clubs.FirstOrDefault(c => c.Id == id)!;
  }

  public async Task<Availability> GetAvailability(string date)
  {
    if (!this.cache.TryGetValue<Availability>("availability", out Availability? availability))
    {
      (int hours, int minutes, int seconds) = (1, 0, 0);
      TimeSpan expiration = new TimeSpan(hours, minutes, seconds);
      availability = this.cache.Set(
          "availability",
          await this.DoGetAvailability(date),
          new MemoryCacheEntryOptions
          {
            AbsoluteExpirationRelativeToNow = expiration
          });
    }
    return availability!;
  }
}
