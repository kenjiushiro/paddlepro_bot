using paddlepro.API.Models.Application;

namespace paddlepro.API.Services.Interfaces;

public interface IPaddleService
{
  Task<Availability> GetAvailability(string date);
  string GetCheckoutUrl(string clubId, string day, string courtId, string start, string duration);
  Club GetClubDetails(string id);
}
