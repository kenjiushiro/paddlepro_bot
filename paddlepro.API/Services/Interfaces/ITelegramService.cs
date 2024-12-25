namespace paddlepro.API.Services.Interfaces;

public interface ITelegramService
{
  Task<bool> HandleWebhook(Telegram.Bot.Types.Update update);
}
