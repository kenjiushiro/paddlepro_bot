namespace paddlepro.API.Services.Interfaces;

public interface ITelegramService
{
  Task<bool> Respond(Telegram.Bot.Types.Update update);
}
