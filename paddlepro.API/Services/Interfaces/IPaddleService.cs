using paddlepro.API.Models.Infrastructure;

namespace paddlepro.API.Services.Interfaces;

public interface IPaddleService
{
    Task<AtcResponse> GetAvailability(string date);
    string GetCheckoutUrl(string clubId, string day, string courtId, string start, string duration);
}
