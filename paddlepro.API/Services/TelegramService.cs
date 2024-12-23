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

  private static Dictionary<string, long?> pollChatIdDict = new Dictionary<string, long?>();
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
      _contextService.SetChatContext(chatId, threadId, "", command);
      await SendAvailableDates(chatId);
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
      // Date selected
      var chatId = update?.CallbackQuery?.Message?.Chat.Id;
      OnDateSelected(update);
      var context = _contextService.GetChatContext(chatId);
      if (context.LastCommand.Contains(_telegramConfig.Commands.ReadyCheck))
      {
        await StartReadyCheckPoll(chatId);
      }
      else if (context.LastCommand.Contains(_telegramConfig.Commands.Search))
      {
        await Search(chatId);
      }
    }
    return true;
  }

  public async Task Search(long? chatId)
  {
    var context = _contextService.GetChatContext(chatId);
    var response = await _botClient.SendMessage(chatId, $"Buscando dia {context.SelectedDate}", messageThreadId: context.MessageThreadId);
  }

  public void OnDateSelected(Update update)
  {
    var chatId = update?.CallbackQuery?.Message?.Chat.Id;
    var threadId = update?.CallbackQuery?.Message?.MessageThreadId;

    var matchDate = update?.CallbackQuery?.Data;

    if (chatId == null)
    {
      _logger.LogWarning("Chat ID null for update {Id} on ReadyCheckPoll step", update.Id);
    }

    if (matchDate == null)
    {
      _logger.LogWarning("Match Date returned null from Update Id {Id}", update.Id);
    }

    var context = _contextService.GetChatContext(chatId);
    context.SelectedDate = matchDate;
  }

  public async Task StartReadyCheckPoll(long? chatId)
  {
    var context = _contextService.GetChatContext(chatId);

    var poll = await _botClient.SendPoll(context.ChatId, $"Estas para jugar el {context.SelectedDate}?", options, isAnonymous: false, messageThreadId: context.MessageThreadId);
    pollChatIdDict.Add(poll.Poll.Id, chatId);
  }

  public async Task SendAvailableDates(long? chatId)
  {
    var dateRange = DateTime.Today.AddDays(_paddleConfig.DaysInAdvance);
    var context = _contextService.GetChatContext(chatId);

    var inlineKeyboard = new InlineKeyboardMarkup();

    DateTime startDate = DateTime.UtcNow;
    for (var i = 0; i < _paddleConfig.DaysInAdvance; i++)
    {
      var date = startDate.AddDays(i);
      inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData(date.ToString("dddd dd-MM-yyyy", new System.Globalization.CultureInfo("es-ES")), date.ToString("dd-MM-yyyy")));
    }

    var query = await _botClient.SendMessage(chatId, "Elegi dia", messageThreadId: context.MessageThreadId, replyMarkup: inlineKeyboard);
  }

  public async Task HandlePollAnswer(Update update)
  {
    var count = update?.Poll?.Options.Single(o => o.Text == "Si").VoterCount;
    if (!pollChatIdDict.TryGetValue(update.Poll.Id, out var chatId))
    {
      _logger.LogWarning("Poll ID {Id} not found", update.Poll.Id);
      return;
    }
    var context = _contextService.GetChatContext(chatId);

    if (count < _paddleConfig.PlayerCount)
    {
      await _botClient.SendMessage(chatId, $"Faltan {_paddleConfig.PlayerCount - count} votos", messageThreadId: context.MessageThreadId, disableNotification: true);
    }
    else
    {
      await Search(chatId);
    }
  }
}
