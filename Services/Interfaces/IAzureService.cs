using Azure.AI.TextAnalytics;

namespace paddlepro.API.Services.Interfaces;

public interface IAzureService
{
  Task<CategorizedEntityCollection> ExtractEntities(string prompt);
}
