using paddlepro.API.Models;
using Telegram.Bot.Types;

namespace paddlepro.API.Services.Interfaces;

public interface ITelegramService
{
  Task<bool> SendAvailableClubs(UpdateContext context);
  Task<bool> HandleReadyCheckVote(Update update);
  Task<bool> SendAvailableDates(Update update, string nextStep);
  Task<bool> SendPinnedMatchReminderMessage(Update update);
  Task<bool> HandleDatePick(Update update);
  Task<bool> HandleCourtPick(Update update);
  Task<bool> HandleClubPick(Update update);
  Task<bool> HandleHourPick(Update update);
  Task<bool> BookCourt(Update update);
}
