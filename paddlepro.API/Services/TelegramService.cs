using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace paddlepro.API.Services;

public class TelegramService : ITelegramService
{
    ITelegramBotClient _botClient;
    ILogger<TelegramService> _logger;

    private static Dictionary<string, Message> pollChatDict = new Dictionary<string, Message>();
    List<InputPollOption> options = new List<InputPollOption> { new InputPollOption("Si"), new InputPollOption("No") };
    private const int PLAYER_COUNT = 4;
    private const int DAYS = 7;

    public TelegramService(
        ITelegramBotClient botClient,
        ILogger<TelegramService> logger
        )
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task<bool> Respond(Update update)
    {
        if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
        {
            var command = update.Message?.Text ?? "";
            _logger.LogInformation("Command: {Command}", command);

            // TODO move commands to config file
            if (command.Contains("readycheck"))
            {
                await SendAvailableDates(update);
            }
            else if (command.Contains("buscar"))
            {
                await Search(update);
            }
            else if (command.Contains("reservar"))
            {
                await Book(update);
            }
        }
        else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Poll)
        {
            await HandlePollAnswer(update);
        }
        else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.PollAnswer)
        {
        }
        else
        {
            _logger.LogInformation("Type {type}", update.Type);
            await ReadyCheckPoll(update);
        }
        return true;
    }

    public async Task Search(Update update)
    {
        var chatId = update?.Message?.Chat.Id;
        var response = await _botClient.SendMessage(chatId, "Buscando");
    }

    public async Task Book(Update update)
    {
        var chatId = update?.Message?.Chat.Id;
        var threadId = update?.Message?.MessageThreadId;
        var response = await _botClient.SendMessage(chatId, "Reservando");

        await _botClient.SendMessage(chatId, "Link de reserva", messageThreadId: threadId);
    }

    public async Task ReadyCheckPoll(Update update)
    {
        var matchDate = update?.CallbackQuery?.Data;

        _logger.LogInformation("CallbackQuery.Message: {Message}", update?.CallbackQuery?.Message);
        _logger.LogInformation("Update.Id: {Id}", update?.Id);

        _logger.LogInformation("CallbackQuery ID: {Id}", update?.CallbackQuery?.Id);

        var chatId = update?.Message?.Chat?.Id;
        if (chatId == null)
        {
            return;
        }
        var threadId = update.Message.MessageThreadId;
        var poll = await _botClient.SendPoll(chatId, $"Estas para jugar el {matchDate}?", options, isAnonymous: false, messageThreadId: threadId);
        pollChatDict.Add(poll.Poll.Id, update.Message);
    }

    public async Task SendAvailableDates(Update update)
    {
        var chatId = update?.Message?.Chat.Id;
        var threadId = update?.Message?.MessageThreadId;
        var dateRange = DateTime.Today.AddDays(DAYS);

        var inlineKeyboard = new InlineKeyboardMarkup();

        DateTime startDate = DateTime.UtcNow;
        for (var i = 0; i < DAYS; i++)
        {
            var date = startDate.AddDays(i);
            inlineKeyboard.AddButton(InlineKeyboardButton.WithCallbackData(date.ToString("dddd dd-MM-yyyy"), date.ToString("dd-MM-yyyy")));
        }

        var query = await _botClient.SendMessage(chatId, "Elegi dia", messageThreadId: threadId, replyMarkup: inlineKeyboard);
        _logger.LogInformation("Keyboard response Id: {Id}", query.Id);
    }

    public async Task HandlePollAnswer(Update update)
    {
        var count = update?.Poll?.Options.Single(o => o.Text == "Si").VoterCount;
        if (!pollChatDict.TryGetValue(update.Poll.Id, out var message))
        {
            return;
        }
        var chatId = message.Chat.Id;
        var threadId = message.MessageThreadId;

        if (count < PLAYER_COUNT)
        {
            await _botClient.SendMessage(chatId, $"Faltan {PLAYER_COUNT - count} votos", messageThreadId: threadId);
        }
        else
        {
            await Search(update);
        }
        _logger.LogInformation("Poll Id :{Id}", update.Poll.Id);
        _logger.LogInformation("Si count :{Count}", count);
    }
}
