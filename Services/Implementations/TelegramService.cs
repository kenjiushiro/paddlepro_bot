using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using paddlepro.API.Models;
using paddlepro.API.Helpers;
using paddlepro.API.Services.Interfaces;
using paddlepro.API.Models.Application;
using AutoMapper;
using paddlepro.API.Configurations;
using Telegram.Bot.Types;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace paddlepro.API.Services.Implementations;

public class TelegramService : ITelegramService
{
  private readonly ITelegramBotClient botClient;
  private readonly ILogger<TelegramService> logger;
  private readonly AtcServiceConfiguration paddleConfig;
  private readonly TelegramConfiguration telegramConfig;
  private readonly IPaddleService paddleService;
  private readonly IWeatherService weatherService;
  private readonly IAzureService azureService;
  private readonly IUpdateContextService contextService;
  private readonly IMemoryCache cache;
  private readonly IMapper mapper;
  private readonly List<InputPollOption> options = new List<InputPollOption> { new InputPollOption("Si"), new InputPollOption("No") };

  public TelegramService(
      ITelegramBotClient botClient,
      ILogger<TelegramService> logger,
      IOptions<AtcServiceConfiguration> paddleConfig,
      IOptions<TelegramConfiguration> telegramConfig,
      IPaddleService paddleService,
      IWeatherService weatherService,
      IUpdateContextService contextService,
      IMemoryCache cache,
      IMapper mapper,
      IAzureService azureService
    )
  {
    this.botClient = botClient;
    this.logger = logger;
    this.paddleConfig = paddleConfig.Value;
    this.telegramConfig = telegramConfig.Value;
    this.paddleService = paddleService;
    this.weatherService = weatherService;
    this.contextService = contextService;
    this.azureService = azureService;
    this.cache = cache;
    this.mapper = mapper;
  }

  public static DateTime ParseDate(string input)
  {
    var today = DateTime.Today;

    DateTime GetNextDayOccurrence(DayOfWeek dayOfWeek)
    {
      var day = DateTime.Today.AddDays(1);
      while (day.DayOfWeek != dayOfWeek)
      {
        day = day.AddDays(1);
      }
      return day;
    }

    Dictionary<string, DateTime> days = new Dictionary<string, DateTime> {
      { "hoy", today },
      { "mañana", today.AddDays(1) },
      { "lunes", GetNextDayOccurrence(DayOfWeek.Monday) },
      { "martes", GetNextDayOccurrence(DayOfWeek.Tuesday) },
      { "miercoles", GetNextDayOccurrence(DayOfWeek.Wednesday) },
      { "jueves", GetNextDayOccurrence(DayOfWeek.Thursday) },
      { "viernes", GetNextDayOccurrence(DayOfWeek.Friday) },
      { "sabado", GetNextDayOccurrence(DayOfWeek.Saturday) },
      { "domingo", GetNextDayOccurrence(DayOfWeek.Sunday) },
    };

    var dayString = days.Keys.First(key => input.Contains(key));
    var day = days[dayString];
    if (string.IsNullOrEmpty(dayString))
    {
      return DateTime.Now;
    }

    string pattern = @"\b(2[0-4]|1[0-9]|0?[0-9])\b";
    Match match = Regex.Match(input, pattern);

    if (!match.Success)
    {
      return DateTime.Now;
    }
    var hour = int.Parse(match.Value);
    if (hour < 12 && input.ToLower().Contains("pm"))
    {
      hour += 12;
    }

    var minutes = 0;
    if (input.Contains("30"))
    {
      minutes = 30;
    }

    return day.AddHours(hour).AddMinutes(minutes);
  }

  public async Task<bool> BookCourt(Update update)
  {
    var context = this.contextService.GetChatContext(update);

    var entities = await this.azureService.ExtractEntities(update.Message?.Text!);
    this.logger.LogInformation(update.Message?.Text!);

    var durationEntity = entities.FirstOrDefault(entity => entity.Category == "DateTime" && entity.SubCategory == "Duration");
    var dateEntity = entities.FirstOrDefault(entity => entity.Category == "DateTime" && entity.SubCategory != "Duration");

    this.logger.LogInformation("Duration entity: {Log}", durationEntity);
    this.logger.LogInformation("Date entity: {Log}", dateEntity);

    string duration = durationEntity.Text ?? "90";
    duration = duration.ToLower().Replace("minutos", "").Trim();
    var date = ParseDate(dateEntity.Text);
    var day = date.ToString("yyyy-MM-dd");
    context.SelectedDate = day;

    var hour = date.ToString("HH:mm");

    this.logger.LogInformation("Duration: {Log}", duration);
    this.logger.LogInformation("Day: {Log}", day);
    this.logger.LogInformation("Hour: {Log}", hour);

    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(day));

    foreach (var entity in entities.Where(entity => entity.Category == "Location"))
    {
      var clubs = availability
        .Clubs
        .Where(club =>
            $"{club.Name}{club.Location.Address}".ToLower().Contains(entity.Text.ToLower())
            && club.Courts.Any(c => c.Availability.Any(a => a.Duration.ToString() == duration && a.Start == hour))
        );
      this.logger.LogInformation("Clubs count: {Clubs}", clubs.ToArray().Length);

      foreach (var club in clubs)
      {
        foreach (var court in club.Courts)
        {
          if (court.Availability.Any(a => a.Duration.ToString() == duration && a.Start == hour))
          {
            await SendReservationActions(context, club.Id, court.Id, hour, duration);
          }
        }
      }
    }

    return true;
  }

  public async Task<bool> SendAvailableClubs(UpdateContext context)
  {
    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    var clubs = availability.Clubs.Where(x => this.paddleConfig.ClubIds.ToList().Contains(x.Id) && x.IsAvailable).ToArray();
    context.ClearMessages(BotMessageType.DayPicker);
    var message = clubs.Select(club => GetClubMessage(club, context.SelectedDate)).Where(m => !string.IsNullOrEmpty(m)).Join("\n\n").EscapeCharsForMarkdown();

    var buttons = clubs.Select(c =>
    {
      return new[]
            {
                InlineKeyboardButton.WithCallbackData($"{c.Name}", (Common.PICK_CLUB_COMMAND, c.Id).EncodeCallback()),
            };
    });

    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons);
    var response = await this.botClient.SendMessage(
        context.ChatId!,
        message,
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2);
    context.AddMessage(response.Id, BotMessageType.ClubMessage);
    return true;
  }

  public async Task<bool> HandleReadyCheckVote(Update update)
  {
    var count = update?.Poll?.Options.Single(o => o.Text == "Si").VoterCount;
    this.logger.LogInformation("Handling poll");
    if (!Common.pollChatIdDict.TryGetValue(update?.Poll?.Id!, out var chatId))
    {
      this.logger.LogWarning("Poll ID {Id} not found", update?.Poll?.Id);
      return false;
    }
    var context = this.contextService.GetChatContext(update!);

    if (count < this.paddleConfig.PlayerCount)
    {
      var votosFaltantes = this.paddleConfig.PlayerCount - count;
      var messageText = votosFaltantes == 1 ? "Falta 1 voto" : $"Faltan {votosFaltantes} votos";
      await this.botClient.EditMessageText(context.ChatId!, context.GetMessages(BotMessageType.CountMessage).First(), messageText);
      return true;
    }
    else
    {
      await this.SendAvailableClubs(context);
      return true;
    }
  }

  private string GetAvailabilityMessage(Models.Application.Court court)
  {
    return court.Availability.GroupBy(a => a.Start).Select(a => @$"
>{a.Key} - {a.Select(b => b.Duration + "min").Join(" - ")}").Join("");
  }

  private string GetCourtMessage(Models.Application.Court court)
  {
    var roofed = court.IsRoofed ? "Techada" : "No techada";

    return @$"🎾*{court.Name}*
**>{roofed} {GetAvailabilityMessage(court)}||
";
  }

  private string GetClubMessage(Club club, string date)
  {
    var hoursAvailable = club
      .Courts
      .SelectMany(c => c.Availability)
      .GroupBy(a => a.Start)
      .Select(a => $">{a.Key} {a.Select(b => b.Duration.ToString() + "min").Distinct().Join(" - ")}")
      .Join("\n>");
    return @$"
*🏟️ {club.Name}
📍 {club.Location.Address}*
**>
{hoursAvailable} ||";
  }

  private async Task DeleteMessages(UpdateContext context, BotMessageType type)
  {
    var messageToDelete = context.GetMessages(type);
    if (this.telegramConfig.DeleteMessages && messageToDelete.Length > 0)
    {
      await this.botClient.DeleteMessages(context.ChatId!, messageToDelete);
      context.ClearMessages(type);
    }
  }

  public async Task<bool> SendAvailableDates(Update update, string nextStep)
  {
    var context = this.contextService.GetChatContext(update);
    context.NextStep = nextStep;
    var dateRange = DateTime.Today.AddDays(this.paddleConfig.DaysInAdvance);
    var forecast = await this.weatherService.GetWeatherForecast();

    await this.DeleteMessages(context, BotMessageType.DayPicker);

    var inlineKeyboard = new InlineKeyboardMarkup();

    DateTime startDate = DateTime.UtcNow;
    for (var i = 0; i < this.paddleConfig.DaysInAdvance; i++)
    {
      var date = startDate.AddDays(i);
      var buttonDisplay = date.ToString("dddd dd-MM", new System.Globalization.CultureInfo("es-ES"));
      var buttonValue = date.ToString("yyyy-MM-dd");
      var dayForecast = forecast.SingleOrDefault(f => f.Day == buttonValue);

      var rainEmoji = "";

      buttonDisplay = $"{buttonDisplay} {dayForecast?.Emoji} {dayForecast?.MinTemp}°C-{dayForecast?.MaxTemp}°C {rainEmoji} {dayForecast?.ChanceOfRain}%";
      inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData(buttonDisplay, (Common.PICK_DATE_COMMAND, buttonValue).EncodeCallback()));
    }
    this.logger.LogInformation("ChatId: {ChatId}", context.ChatId);
    this.logger.LogInformation("ThreadId: {ThreadId}", context.MessageThreadId);
    this.logger.LogInformation("Context: {Context}", context);

    var message = await this.botClient.SendMessage(context.ChatId!, "Elegi dia", messageThreadId: context.MessageThreadId, replyMarkup: inlineKeyboard, disableNotification: true);
    context.AddMessage(message.MessageId, BotMessageType.DayPicker);
    return true;
  }

  private async Task OnDateSelected(Update update, string date)
  {
    var chatId = update?.CallbackQuery?.Message?.Chat.Id;
    var threadId = update?.CallbackQuery?.Message?.MessageThreadId;

    var matchDate = date;

    if (chatId == null)
    {
      this.logger.LogWarning("Chat ID null for update {Id} on ReadyCheckPoll step", update?.Id);
    }

    if (matchDate == null)
    {
      this.logger.LogWarning("Match Date returned null from Update Id {Id}", update?.Id);
    }

    var context = this.contextService.GetChatContext(update!);
    context.SelectedDate = matchDate!;
    await this.DeleteMessages(context, BotMessageType.DayPicker);
  }

  private async Task<bool> StartReadyCheckPoll(UpdateContext context)
  {
    var poll = await this.botClient.SendPoll(context?.ChatId!, $"Estas para jugar el {context?.SelectedDate}?", options, isAnonymous: false, messageThreadId: context?.MessageThreadId, disableNotification: true);
    context?.AddMessage(poll.MessageId, BotMessageType.ReadyCheckPoll);
    Common.pollChatIdDict.Add(poll.Poll?.Id!, context?.ChatId);
    await SendPlayerCountMessage(context!);
    return true;
  }

  private async Task SendPlayerCountMessage(UpdateContext context, string messageText = "Faltan 4 votos")
  {
    var countMessage = await this.botClient.SendMessage(context.ChatId!, messageText, messageThreadId: context.MessageThreadId, disableNotification: true);
    context.AddMessage(countMessage.MessageId, BotMessageType.CountMessage);
  }

  public async Task<bool> HandleCourtPick(Update update)
  {
    var context = this.contextService.GetChatContext(update);

    await this.DeleteMessages(context, BotMessageType.CourtMessage);

    (var _, var selection) = (update?.CallbackQuery?.Data!).DecodeCallback();
    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    (var clubId, var courtId) = selection.SplitBy(":");
    var club = availability.Clubs.FirstOrDefault(c => c.Id == clubId);
    var court = club?.Courts.FirstOrDefault(c => c.Id == courtId);

    var buttons = court?.Availability.GroupBy(a => a.Start).Select(
        g => g.Select(a =>
          InlineKeyboardButton.WithCallbackData($"{a.Start} {a.Duration}'", (Common.PICK_HOUR_COMMAND, $"{clubId}+{courtId}+{a.Start}+{a.Duration}").EncodeCallback())
                      )
        );

    var message = @$"📅*{context.SelectedDate}*
🏟️{club?.Name}
🎾_{court?.Name}_ ";
    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons!);
    var response = await this.botClient.SendMessage(
        context.ChatId!,
        message.EscapeCharsForMarkdown(),
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2
        );
    context.AddMessage(response.Id, BotMessageType.HourPicker);
    return true;
  }

  public async Task<bool> HandleClubPick(Update update)
  {
    var context = this.contextService.GetChatContext(update);

    await this.DeleteMessages(context, BotMessageType.ClubMessage);

    (var _, var clubId) = (update?.CallbackQuery?.Data!).DecodeCallback();
    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    var club = availability.Clubs.FirstOrDefault(c => c.Id == clubId);
    var courts = club.Courts.Where(c => c.IsAvailable);

    var buttons = courts.Select(
        court =>
        {
          var value = $"{clubId}:{court.Id}";
          return new[]
                  {
                    InlineKeyboardButton.WithCallbackData($"{court.Name}", (Common.PICK_COURT_COMMAND, value).EncodeCallback()),
          };
        }).ToArray();


    var message = @$"📅 {context.SelectedDate}
🏟️{club?.Name}
{courts.Select(c => GetCourtMessage(c)).Join("\n")}";

    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons!);
    var response = await this.botClient.SendMessage(
        context.ChatId!,
        message.EscapeCharsForMarkdown(),
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2
        );
    context.AddMessage(response.Id, BotMessageType.CourtMessage);
    return true;
  }

  public async Task<bool> HandleHourPick(Update update)
  {
    var context = this.contextService.GetChatContext(update);

    (var _, var selection) = (update?.CallbackQuery?.Data!).DecodeCallback();

    (var clubId, var courtId, var start, var duration) = selection.SplitBy4("+");

    return await SendReservationActions(context, clubId, courtId, start, duration);
  }

  public async Task<bool> SendReservationActions(UpdateContext context, string clubId, string courtId, string start, string duration)
  {
    await this.DeleteMessages(context, BotMessageType.HourPicker);
    string callbackValue = $"{clubId}+{courtId}+{start}+{duration}";

    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup();
    var url = this.paddleService.GetCheckoutUrl(clubId, context.SelectedDate, courtId, start, duration);
    inlineKeyboard.AddNewRow(InlineKeyboardButton.WithUrl("📅Reservar", url));
    inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("📍Pinear mensaje", (Common.PIN_REMINDER_COMMAND, callbackValue).EncodeCallback()));
    var club = this.paddleService.GetClubDetails(clubId);
    var court = club.Courts.FirstOrDefault(c => c.Id == courtId);

    var response = await this.botClient.SendMessage(
        context.ChatId!,
        text: @$"Reservar
📅{context.SelectedDate} 🕒{start} ⏱️{duration}min
🏟️{club.Name}
🎾{court.Name}
",
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard
        );
    return true;
  }

  public async Task<bool> HandleDatePick(Update update)
  {
    var context = this.contextService.GetChatContext(update);
    await this.DeleteMessages(context, BotMessageType.DayPicker);

    (var _, var selection) = (update?.CallbackQuery?.Data!).DecodeCallback();
    await OnDateSelected(update!, selection);
    this.logger.LogInformation("Selected date: {SelectedDate} Next step: {NextStep}", selection, context.NextStep);

    if (context.NextStep == "readyCheck")
    {
      return await StartReadyCheckPoll(context);
    }
    else if (context.NextStep == "search")
    {
      return await SendAvailableClubs(context);
    }
    return false;
  }

  public async Task<bool> SendPinnedMatchReminderMessage(Update update)
  {
    var context = this.contextService.GetChatContext(update);
    (var _, var selection) = (update?.CallbackQuery?.Data!).DecodeCallback();

    (var clubId, var courtId, var start, var duration) = selection.SplitBy4("+");
    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    var club = availability.Clubs.FirstOrDefault(c => c.Id == clubId);
    var court = club?.Courts.FirstOrDefault(c => c.Id == courtId);

    var message = @$"Detalles del partido:
📅 *{context.SelectedDate}*
📍 {club?.Location.Address}
🏟️ {club?.Name}
🎾 {court?.Name}
🕒{start}
⏱️{duration}min
        ";

    var response = await this.botClient.SendMessage(
        context.ChatId!,
        message.EscapeCharsForMarkdown(),
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2
        );
    await this.botClient.PinChatMessage(context.ChatId!, response.MessageId, disableNotification: true);
    return true;
  }
}
