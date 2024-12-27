namespace paddlepro.API.Services.Interfaces;

public interface IAzureService
{
    Task<string> ExtractEntities(string prompt);
}
