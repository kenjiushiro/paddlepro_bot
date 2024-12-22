using paddlepro.API.Models;

namespace paddlepro.API.Services;

public interface IPaddleService
{
    Club[] GetAvailabilities(DateTime date);
}
