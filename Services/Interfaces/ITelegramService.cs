using paddlepro.API.Models;
using Telegram.Bot.Types;

namespace paddlepro.API.Services.Interfaces;

public interface ITelegramService
{
  Task<bool> SendAvailableClubs(UpdateContext context);
  Task<bool> HandleReadyCheckVote(Update update);
  Task<bool> SendDatePicker(Update update, string nextStep);
  Task<bool> HandlerPinReminderPick(Update update, string callbackDate);
  Task<bool> HandleDatePick(Update update, string callbackData);
  Task<bool> HandleCourtPick(Update update, string callbackData);
  Task<bool> HandleClubPick(Update update, string callbackData);
  Task<bool> HandleHourPick(Update update, string callbackData);
  Task<bool> BookCourt(Update update);
}
