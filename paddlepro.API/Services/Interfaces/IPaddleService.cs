using paddlepro.API.Models.Infrastructure;

namespace paddlepro.API.Services.Interfaces;

public interface IPaddleService
{
  Task<AtcResponse> GetAvailability(string date);
}
