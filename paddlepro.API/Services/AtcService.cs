using paddlepro.API.Models;

namespace paddlepro.API.Services;

public class AtcService : IPaddleService
{
    ILogger<AtcService> _logger;

    public AtcService(ILogger<AtcService> logger)
    {
        _logger = logger;
    }

    public Club[] GetAvailabilities(DateTime date)
    {
        throw new NotImplementedException();
    }
}
