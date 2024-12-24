using paddlepro.API.Models;
using paddlepro.API.Configurations;
using paddlepro.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace paddlepro.API.Services.Implementations;

public class AtcService : IPaddleService
{
    private readonly ILogger<AtcService> _logger;
    private readonly HttpClient _client;
    private readonly PaddleServiceConfiguration _config;

    public AtcService(
        ILogger<AtcService> logger,
        HttpClient client,
        IOptions<PaddleServiceConfiguration> config
      )
    {
        _logger = logger;
        _client = client;
        _config = config.Value;
    }

    public Club[] GetAvailabilities(DateTime date)
    {
        throw new NotImplementedException();
    }
}
