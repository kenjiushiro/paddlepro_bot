using paddlepro.API.Services.Interfaces;
using Azure.AI.TextAnalytics;
using Azure;
using Microsoft.Extensions.Options;
using paddlepro.API.Configurations;

namespace paddlepro.API.Services.Implementations;

public class AzureService : IAzureService
{
  private readonly AzureConfiguration azureConfig;

  public AzureService(
      IOptions<AzureConfiguration> azureConfig
      )
  {
    this.azureConfig = azureConfig.Value;
  }

  static string languageKey = "ALnOMPnwMMJI53s817vkz1CkiyEgbbkMmdrt6JsIiF6Z9Jnz7lBNJQQJ99ALACYeBjFXJ3w3AAAaACOGfN1u";
  static string languageEndpoint = "https://kenjilanguage.cognitiveservices.azure.com/";

  private static readonly AzureKeyCredential credentials = new AzureKeyCredential(languageKey);
  private static readonly Uri endpoint = new Uri(languageEndpoint);

  // Example method for extracting named entities from text 
  static async Task<CategorizedEntityCollection> EntityRecognitionExample(TextAnalyticsClient client, string prompt)
  {
    var response = await client.RecognizeEntitiesAsync(prompt);
    return response;
  }

  public async Task<CategorizedEntityCollection> ExtractEntities(string prompt)
  {
    var client = new TextAnalyticsClient(endpoint, credentials);
    return await EntityRecognitionExample(client, prompt);
  }
}
