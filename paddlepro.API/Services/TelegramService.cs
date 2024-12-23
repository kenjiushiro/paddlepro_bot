using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using paddlepro.API.Configurations;

namespace paddlepro.API.Services;

public class TelegramService : ITelegramService
{
  ITelegramBotClient _botClient;
  ILogger<TelegramService> _logger;
  PaddleServiceConfiguration _paddleConfig;
  TelegramConfiguration _telegramConfig;
  IPaddleService _paddleService;
  IContextService _contextService;
  IWeatherService _weatherService;

  private static Dictionary<string, Message> pollMessageDict = new Dictionary<string, Message>();
  List<InputPollOption> options = new List<InputPollOption> { new InputPollOption("Si"), new InputPollOption("No") };

  public TelegramService(
      ITelegramBotClient botClient,
      IPaddleService paddleService,
      IWeatherService weatherService,
      IContextService contextService,
      ILogger<TelegramService> logger,
      IOptions<PaddleServiceConfiguration> paddleConfig,
      IOptions<TelegramConfiguration> telegramConfig
      )
  {
    _botClient = botClient;
    _logger = logger;
    _contextService = contextService;
    _paddleService = paddleService;
    _weatherService = weatherService;
    _paddleConfig = paddleConfig.Value;
    _telegramConfig = telegramConfig.Value;
  }

  public async Task<bool> Respond(Update update)
  {
    if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
    {
      var chatId = update?.Message?.Chat.Id;
      var threadId = update?.Message?.MessageThreadId;
      var command = update?.Message?.Text ?? "";
      _logger.LogInformation("Command: {Command}", command);

      // TODO move commands text to config file
      if (command.Contains(_telegramConfig.Commands.ReadyCheck))
      {
        _contextService.SetChatContext(chatId, threadId, "", command);
        await SendAvailableDates(chatId, threadId);
      }
      else if (command.Contains(_telegramConfig.Commands.Search))
      {
        await Search(chatId, threadId);
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

  public async Task Search(long? chatId, int? threadId)
  {
    var response = await _botClient.SendMessage(chatId, "Buscando", messageThreadId: threadId);
  }

  public async Task<(long?, int?, string)> OnDateSelected(Update update)
  {
    var matchDate = update?.CallbackQuery?.Data;

    var chatId = update?.CallbackQuery?.Message?.Chat.Id;

    if (chatId == null)
    {
      _logger.LogWarning("Chat ID null for update {Id} on ReadyCheckPoll step", update.Id);
      return (0, 0, "");
    }
    var threadId = update?.CallbackQuery?.Message?.MessageThreadId;
    return (chatId, threadId, matchDate);
  }

  public async Task ReadyCheckPoll(Update update)
  {
    (long? chatId, int? threadId, string matchDate) = await OnDateSelected(update);
    var poll = await _botClient.SendPoll(chatId, $"Estas para jugar el {matchDate}?", options, isAnonymous: false, messageThreadId: threadId);
    pollMessageDict.Add(poll.Poll.Id, update?.CallbackQuery?.Message);
  }

  public async Task SendAvailableDates(long? chatId, int? threadId)
  {
    var dateRange = DateTime.Today.AddDays(_paddleConfig.DaysInAdvance);

    var inlineKeyboard = new InlineKeyboardMarkup();

    DateTime startDate = DateTime.UtcNow;
    for (var i = 0; i < _paddleConfig.DaysInAdvance; i++)
    {
      var date = startDate.AddDays(i);
      inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData(date.ToString("dddd dd-MM-yyyy", new System.Globalization.CultureInfo("es-ES")), date.ToString("dd-MM-yyyy")));
    }

    var query = await _botClient.SendMessage(chatId, "Elegi dia", messageThreadId: threadId, replyMarkup: inlineKeyboard);
  }

  public async Task HandlePollAnswer(Update update)
  {
    var count = update?.Poll?.Options.Single(o => o.Text == "Si").VoterCount;
    if (!pollMessageDict.TryGetValue(update.Poll.Id, out var message))
    {
      _logger.LogWarning("Poll ID {Id} not found", update.Poll.Id);
      return;
    }
    var chatId = message.Chat.Id;
    var threadId = message.MessageThreadId;

    if (count < _paddleConfig.PlayerCount)
    {
      await _botClient.SendMessage(chatId, $"Faltan {_paddleConfig.PlayerCount - count} votos", messageThreadId: threadId, disableNotification: true);
    }
    else
    {
      await Search(chatId, threadId);
    }
  }
}
