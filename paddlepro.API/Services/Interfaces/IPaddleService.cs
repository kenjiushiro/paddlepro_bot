using paddlepro.API.Models;

namespace paddlepro.API.Services.Interfaces;

public interface IPaddleService
{
  Task<Club[]> GetAvailabilities(string date);
}
