using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using paddlepro.API.Configurations;
using paddlepro.API.Models;
using paddlepro.API.Services.Interfaces;

namespace paddlepro.API.Services.Implementations;

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

      if (!command.Contains(_telegramConfig.Commands.ReadyCheck) && !command.Contains(_telegramConfig.Commands.Search))
      {
        return false;
      }
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
      await OnDateSelected(update);
      var context = _contextService.GetChatContext(chatId);
      if (context.LastCommand.Contains(_telegramConfig.Commands.ReadyCheck))
      {
        await StartReadyCheckPoll(chatId);
      }
      else if (context.LastCommand.Contains(_telegramConfig.Commands.Search))
      {
        await Search(context);
      }
    }
    return true;
  }

  public async Task Search(Context context)
  {
    var availability = await _paddleService.GetAvailabilities(context.SelectedDate);
    var response = await _botClient.SendMessage(context.ChatId, $"Buscando dia {context.SelectedDate}", messageThreadId: context.MessageThreadId);
  }

  public async Task SetDate(long? chatId)
  {
    var context = _contextService.GetChatContext(chatId);
    var message = await _botClient.SendMessage(chatId, $"Se juega x dia", messageThreadId: context.MessageThreadId);
    await _botClient.PinChatMessage(chatId, message.MessageId);
  }

  public async Task OnDateSelected(Update update)
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
    if (context.LatestDayPicker > 0)
    {
      _botClient.DeleteMessage(context.ChatId, context.LatestDayPicker);
      context.LatestDayPicker = 0;
    }
  }

  public async Task StartReadyCheckPoll(long? chatId)
  {
    var context = _contextService.GetChatContext(chatId);
    if (context.LatestPollId > 0)
    {
      _botClient.DeleteMessage(context.ChatId, context.LatestPollId);
      context.LatestPollId = 0;
    }

    var poll = await _botClient.SendPoll(context.ChatId, $"Estas para jugar el {context.SelectedDate}?", options, isAnonymous: false, messageThreadId: context.MessageThreadId, disableNotification: true);
    context.LatestPollId = poll.MessageId;
    pollChatIdDict.Add(poll.Poll.Id, chatId);
    await SendPlayerCountMessage(context);
  }

  public async Task SendAvailableDates(long? chatId)
  {
    var dateRange = DateTime.Today.AddDays(_paddleConfig.DaysInAdvance);
    var forecast = await _weatherService.GetWeatherForecast();

    var context = _contextService.GetChatContext(chatId);

    var inlineKeyboard = new InlineKeyboardMarkup();

    DateTime startDate = DateTime.UtcNow;
    for (var i = 0; i < _paddleConfig.DaysInAdvance; i++)
    {
      var date = startDate.AddDays(i);
      var buttonDisplay = date.ToString("dddd dd-MM", new System.Globalization.CultureInfo("es-ES"));
      var buttonValue = date.ToString("yyyy-MM-dd");
      var dayForecast = forecast.SingleOrDefault(f => f.Day == buttonValue);

      var rainEmoji = "";

      buttonDisplay = $"{buttonDisplay} {dayForecast.Emoji} {dayForecast.MinTemp}°C-{dayForecast.MaxTemp}°C {rainEmoji} {dayForecast.ChanceOfRain}%";
      inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData(buttonDisplay, buttonValue));
    }

    var message = await _botClient.SendMessage(chatId, "Elegi dia", messageThreadId: context.MessageThreadId, replyMarkup: inlineKeyboard);
    context.LatestDayPicker = message.MessageId;
  }

  private async Task SendPlayerCountMessage(Context context, string messageText = "Faltan 4 votos")
  {
    var countMessage = await _botClient.SendMessage(context.ChatId, messageText, messageThreadId: context.MessageThreadId, disableNotification: true);
    context.CountMessageId = countMessage.MessageId;
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
      var messageText = $"Faltan {_paddleConfig.PlayerCount - count} votos";
      await _botClient.EditMessageText(context.ChatId, context.CountMessageId, messageText);
    }
    else
    {
      await Search(context);
    }
  }
}
