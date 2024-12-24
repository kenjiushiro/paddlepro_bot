using paddlepro.API.Models;

namespace paddlepro.API.Services.Interfaces;

public interface IPaddleService
{
  Club[] GetAvailabilities(DateTime date);
}
