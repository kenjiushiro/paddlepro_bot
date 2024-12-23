namespace paddlepro.API.Services;

public interface ITelegramService
{
    Task<bool> Respond(Telegram.Bot.Types.Update update);
}
