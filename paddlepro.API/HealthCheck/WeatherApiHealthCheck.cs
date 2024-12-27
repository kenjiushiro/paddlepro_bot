using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using paddlepro.API.Configurations;
using paddlepro.API.Helpers;

namespace paddlepro.API.HealthCheck;

public class WeatherServiceHealthCheck : IHealthCheck
{
    private readonly HttpClient httpClient;
    private readonly WeatherServiceConfiguration config;

    public WeatherServiceHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<WeatherServiceConfiguration> config
        )
    {
        this.httpClient = httpClientFactory.CreateClient("WeatherServiceHealth");
        this.config = config.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new Dictionary<string, string> {
              {"q", "Buenos%20Aires"},
              {"days", this.config.DaysInAdvance.ToString()},
              {"key", this.config.ApiKey}
            };

            var response = await this.httpClient.GetAsync($"/v1/forecast.json{queryParams.ToQueryParams()}");

            var data = new Dictionary<string, object>
            {
                { "Endpoint", "forecast.json" },
                { "StatusCode", (int)response.StatusCode }
            };

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("OK", data)
                : HealthCheckResult.Unhealthy($"Weather API failed {response.StatusCode}", null, data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Weather API health check failed", ex);
        }
    }
}
