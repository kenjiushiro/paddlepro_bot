using paddlepro.API.Services.Interfaces;
using Azure.AI.TextAnalytics;

namespace paddlepro.API.Services.Implementations;

public class AzureService : IAzureService
{
    public AzureService()
    {
    }

    public Task<string> ExtractEntities(string prompt)
    {
        throw new NotImplementedException();
    }
}
