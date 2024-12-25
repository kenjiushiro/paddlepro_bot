using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using paddlepro.API.Configurations;
using paddlepro.API.Models;
using paddlepro.API.Helpers;
using paddlepro.API.Services.Interfaces;
using paddlepro.API.Models.Application;
using AutoMapper;

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
  IMapper _mapper;

  private static Dictionary<string, long?> pollChatIdDict = new Dictionary<string, long?>();
  List<InputPollOption> options = new List<InputPollOption> { new InputPollOption("Si"), new InputPollOption("No") };

  public TelegramService(
      ITelegramBotClient botClient,
      IPaddleService paddleService,
      IWeatherService weatherService,
      IContextService contextService,
      ILogger<TelegramService> logger,
      IOptions<PaddleServiceConfiguration> paddleConfig,
      IOptions<TelegramConfiguration> telegramConfig,
      IMapper mapper
      )
  {
    _botClient = botClient;
    _logger = logger;
    _contextService = contextService;
    _paddleService = paddleService;
    _weatherService = weatherService;
    _paddleConfig = paddleConfig.Value;
    _telegramConfig = telegramConfig.Value;
    _mapper = mapper;
  }

  private const string PICK_COURT_COMMAND = "CourtPick";
  private const string PICK_HOUR_COMMAND = "HourPick";
  private const string PICK_DATE_COMMAND = "DatePick";
  private const string SCHEDULE_REMINDER_COMMAND = "ScheduleReminder";

  public async Task<bool> HandleWebhook(Update update)
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
    else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
    {
      // Date selected
      var chatId = update?.CallbackQuery?.Message?.Chat.Id;
      var context = _contextService.GetChatContext(chatId);
      (var action, var callbackValue) = DecodeCallback(update?.CallbackQuery?.Data);
      _logger.LogInformation("Action: {action}", action);

      if (action == PICK_DATE_COMMAND)
      {
        await HandleDatePick(update, context, callbackValue);
      }
      else if (action == PICK_COURT_COMMAND)
      {
        await HandleCourtPick(update, context, callbackValue);
      }
      else if (action == PICK_HOUR_COMMAND)
      {
        await HandleStartPick(update, context, callbackValue);
      }
      else if (action == SCHEDULE_REMINDER_COMMAND)
      {
        await ScheduleReminder(update, context, callbackValue);
      }

    }
    return true;
  }

  private async Task HandleDatePick(Update update, Context context, string selection)
  {
    await OnDateSelected(update, selection);
    if (context.LastCommand.Contains(_telegramConfig.Commands.ReadyCheck))
    {
      await StartReadyCheckPoll(chatId);
    }
    else if (context.LastCommand.Contains(_telegramConfig.Commands.Search))
    {
      await Search(context);
    }

  }

  private async Task HandleCourtPick(Update update, Context context, string selection)
  {
    var availability = _mapper.Map<Availability>(await _paddleService.GetAvailability(context.SelectedDate));
    (var clubId, var courtId) = selection.SplitBy(":");
    var club = availability.Clubs.FirstOrDefault(c => c.Id == clubId);
    var court = club.Courts.FirstOrDefault(c => c.Id == courtId);

    var buttons = court.Availability.Select(
        a =>
        {
          var value = $"{clubId}+{courtId}+{a.Start}+{a.Duration}";
          return new[]
                  {
                    InlineKeyboardButton.WithCallbackData($"{a.Start} {a.Duration}min ${a.Price}", EncodeCallback(PICK_HOUR_COMMAND, value)),
          };
        }).ToArray();

    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons);
    var response = await _botClient.SendMessage(
        context.ChatId,
        $"Reservar {club.Name} {court.Name}",
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard
        );
  }

  private async Task ScheduleReminder(Update update, Context context, string selection)
  {
    (var clubId, var courtId, var start, var duration) = selection.SplitBy4("+");
    var availability = _mapper.Map<Availability>(await _paddleService.GetAvailability(context.SelectedDate));
    var club = availability.Clubs.FirstOrDefault(c => c.Id == clubId);
    var court = club.Courts.FirstOrDefault(c => c.Id == courtId);

    var message = @$"Detalles del partido:
🏟️ {club.Name} - 📍 {club.Location.Address} - {context.SelectedDate}
{court.Name} - {start} {duration}min
        ";

    var response = await _botClient.SendMessage(
        context.ChatId,
        message,
        messageThreadId: context.MessageThreadId,
        disableNotification: true);
    await _botClient.PinChatMessage(context.ChatId, response.MessageId, disableNotification: true);
  }

  private async Task HandleStartPick(Update update, Context context, string selection)
  {
    (var clubId, var courtId, var start, var duration) = selection.SplitBy4("+");

    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup();
    var url = _paddleService.GetCheckoutUrl(clubId, context.SelectedDate, courtId, start, duration);
    inlineKeyboard.AddNewRow(InlineKeyboardButton.WithUrl("Reservar", url));
    inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("Activar recordatorio", EncodeCallback(SCHEDULE_REMINDER_COMMAND, selection)));

    var response = await _botClient.SendMessage(
        context.ChatId,
        text: $"Reservar {start} {duration}min",
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard
        );
  }

  private string GetAvailabilityMessage(Models.Application.Court court)
  {
    return court.Availability.Select(a => @$"
>{a.Start} - {a.Duration}min - ${a.Price}").Join("");
  }

  private string GetCourtMessage(Models.Application.Court court)
  {
    var roofed = court.IsRoofed ? "Techada" : "No techada";

    return @$">{court.Name} - {roofed} {GetAvailabilityMessage(court)}";
  }

  private string GetClubMessage(Club club, string date)
  {
    return @$"
*🏟️ {club.Name} - 📍 {club.Location.Address} - {date}*
**>Canchas:
{club.Courts.Select(c => GetCourtMessage(c)).Join("\n")}||";
  }

  private async Task Search(Context context)
  {
    var availability = _mapper.Map<Availability>(await _paddleService.GetAvailability(context.SelectedDate));
    var clubs = availability.Clubs.Where(x => _paddleConfig.ClubIds.ToList().Contains(x.Id)).ToArray();

    foreach (var club in clubs)
    {
      if (club.Courts.All(c => c.Availability.Length == 0))
      {
        continue;
      }
      var message = GetClubMessage(club, context.SelectedDate).EscapeCharsForMarkdown();
      var buttons = club.Courts.Where(c => c.Availability.Length > 0).Select(
          c =>
          {
            var value = $"{club.Id}:{c.Id}";
            return new[]
                  {
                InlineKeyboardButton.WithCallbackData($"{club.Name} - {c.Name} ", EncodeCallback(PICK_COURT_COMMAND, value)),
      };

          }
          ).ToArray();
      InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons);
      var response = await _botClient.SendMessage(
          context.ChatId,
          message,
          messageThreadId: context.MessageThreadId,
          disableNotification: true,
          replyMarkup: inlineKeyboard,
          parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2);
    }

  }

  private async Task SetDate(long? chatId)
  {
    var context = _contextService.GetChatContext(chatId);
    var message = await _botClient.SendMessage(chatId, $"Se juega x dia", messageThreadId: context.MessageThreadId);
    await _botClient.PinChatMessage(chatId, message.MessageId);
  }

  private string EncodeCallback(string action, string data)
  {
    return $"{action};{data}";
  }

  private (string, string) DecodeCallback(string callback)
  {
    return callback.SplitBy(";");
  }

  private async Task OnDateSelected(Update update, string date)
  {
    var chatId = update?.CallbackQuery?.Message?.Chat.Id;
    var threadId = update?.CallbackQuery?.Message?.MessageThreadId;

    var matchDate = date;

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
      /*_botClient.DeleteMessage(context.ChatId, context.LatestDayPicker);*/
      context.LatestDayPicker = 0;
    }
  }

  private async Task StartReadyCheckPoll(long? chatId)
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

  private async Task SendAvailableDates(long? chatId)
  {
    var dateRange = DateTime.Today.AddDays(_paddleConfig.DaysInAdvance);
    var forecast = _mapper.Map<WeatherForecast[]>(await _weatherService.GetWeatherForecast());

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

      buttonDisplay = $"{buttonDisplay} {dayForecast?.Emoji} {dayForecast?.MinTemp}°C-{dayForecast?.MaxTemp}°C {rainEmoji} {dayForecast?.ChanceOfRain}%";
      inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData(buttonDisplay, EncodeCallback(PICK_DATE_COMMAND, buttonValue)));
    }

    var message = await _botClient.SendMessage(chatId, "Elegi dia", messageThreadId: context.MessageThreadId, replyMarkup: inlineKeyboard, disableNotification: true);
    context.LatestDayPicker = message.MessageId;
  }

  private async Task SendPlayerCountMessage(Context context, string messageText = "Faltan 4 votos")
  {
    var countMessage = await _botClient.SendMessage(context.ChatId, messageText, messageThreadId: context.MessageThreadId, disableNotification: true);
    context.CountMessageId = countMessage.MessageId;
  }

  private async Task HandlePollAnswer(Update update)
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
