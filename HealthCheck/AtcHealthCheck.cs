using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using paddlepro.API.Configurations;
using paddlepro.API.Helpers;

namespace paddlepro.API.HealthCheck;

public class AtcHealthCheck : IHealthCheck
{
  private readonly HttpClient httpClient;
  private readonly AtcServiceConfiguration config;

  public AtcHealthCheck(
      IHttpClientFactory httpClientFactory,
      IOptions<AtcServiceConfiguration> config
      )
  {
    this.httpClient = httpClientFactory.CreateClient("PaddleServiceHealth");
    this.config = config.Value;
  }

  public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    try
    {
      var queryParams = new Dictionary<string, string>
            {
                { "horario", "19%3A30" },
                { "tipoDeporte", config.SportId },
                { "dia", DateTime.Today.ToString("yyyy-MM-dd") },
                { "placeId", config.PlaceId }
            };
      var listQuery = this.config.ListPath + queryParams.ToQueryParams();

      var response = await this.httpClient.GetAsync(listQuery, cancellationToken);

      var data = new Dictionary<string, object>
            {
                { "Endpoint", "list" },
                { "StatusCode", (int)response.StatusCode }
            };

      return response.IsSuccessStatusCode
          ? HealthCheckResult.Healthy("OK", data)
          : HealthCheckResult.Unhealthy($"Atc failed {response.StatusCode}", null, data);
    }
    catch (Exception ex)
    {
      return HealthCheckResult.Unhealthy("Atc health check failed", ex);
    }
  }
}
