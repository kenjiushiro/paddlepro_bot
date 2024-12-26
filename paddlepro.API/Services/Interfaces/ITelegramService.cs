using paddlepro.API.Models;
using Telegram.Bot.Types;

namespace paddlepro.API.Services.Interfaces;

public interface ITelegramService
{
  Task<bool> SendAvailability(UpdateContext context);
  Task<bool> HandleReadyCheckVote(Update update);
  Task<bool> SendAvailableDates(Update update);
  Task<bool> SendPinnedMatchReminderMessage(Update update);
  Task<bool> HandleDatePick(Update update);
  Task<bool> HandleCourtPick(Update update);
  Task<bool> HandleHourPick(Update update);
}
