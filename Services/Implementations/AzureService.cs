using paddlepro.API.Services.Interfaces;
using Azure.AI.TextAnalytics;
using Azure;
using Microsoft.Extensions.Options;
using paddlepro.API.Configurations;

namespace paddlepro.API.Services.Implementations;

public class AzureService : IAzureService
{
  private readonly AzureConfiguration azureConfig;
  private readonly ILogger<AzureService> logger;

  public AzureService(
      IOptions<AzureConfiguration> azureConfig,
      ILogger<AzureService> logger
      )
  {
    this.azureConfig = azureConfig.Value;
    this.logger = logger;
  }

  static async Task<CategorizedEntityCollection> EntityRecognitionExample(TextAnalyticsClient client, string prompt)
  {
    var response = await client.RecognizeEntitiesAsync(prompt);
    return response;
  }

  public async Task<CategorizedEntityCollection> ExtractEntities(string prompt)
  {
    AzureKeyCredential credentials = new AzureKeyCredential(this.azureConfig.TextAnalytics.Key);
    Uri endpoint = new Uri(this.azureConfig.TextAnalytics.Endpoint);
    var client = new TextAnalyticsClient(endpoint, credentials);
    return await EntityRecognitionExample(client, prompt);
  }
}
