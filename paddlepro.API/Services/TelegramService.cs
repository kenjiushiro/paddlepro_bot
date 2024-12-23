using Telegram.Bot;
using Telegram.Bot.Types;

namespace paddlepro.API.Services;

public class TelegramService : ITelegramService
{
    ITelegramBotClient _botClient;
    ILogger<TelegramService> _logger;

    public TelegramService(
        ITelegramBotClient botClient,
        ILogger<TelegramService> logger
        )
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task<bool> Respond(Telegram.Bot.Types.Update update)
    {
        var chatId = update.Message.Chat.Id;

        _logger.LogInformation("Id: {UpdateId}", update.Id);
        _logger.LogInformation("Type: {Type}", update.Type);

        if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
        {
            _logger.LogInformation("Text: {Text}", update.Message?.Text);

            if (update.Message.Text.Contains("readycheck"))
            {
                var options = new List<InputPollOption> {
          new InputPollOption("Si"),
              new InputPollOption("No")
        };
                await _botClient.SendPollAsync(chatId, "Jugas?", options);
            }
            else if (update.Message.Text.Contains("buscar"))
            {
                var response = await _botClient.SendTextMessageAsync(chatId, "Buscando");
            }
            else if (update.Message.Text.Contains("reservar"))
            {
                var response = await _botClient.SendTextMessageAsync(chatId, "Link de reserva");
            }
        }
        else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Poll)
        {
            _logger.LogInformation("PollId :{PollId}", update.PollAnswer.PollId);
        }
        return true;
    }
}
