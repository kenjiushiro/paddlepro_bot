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
    this.contextService.SetChatContext(update);
    var context = this.contextService.GetChatContext(update);

    var entities = await this.azureService.ExtractEntities(update.Message.Text!);
    this.logger.LogInformation(update.Message.Text!);

    Club selectedClub;

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
            await SendReservationActions(context, club.Id, court.Id, hour, duration, club.Name, court.Name);
          }
        }
      }
    }

    return true;
  }

  public async Task<bool> SendAvailability(UpdateContext context)
  {
    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    var clubs = availability.Clubs.Where(x => this.paddleConfig.ClubIds.ToList().Contains(x.Id)).ToArray();
    context.CourtMessageIds = Array.Empty<int>();
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
                InlineKeyboardButton.WithCallbackData($"{club.Name} - {c.Name} ", (Common.PICK_COURT_COMMAND, value).EncodeCallback()),
      };

          }
          ).ToArray();
      InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons);
      var response = await this.botClient.SendMessage(
          context.ChatId!,
          message,
          messageThreadId: context.MessageThreadId,
          disableNotification: true,
          replyMarkup: inlineKeyboard,
          parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2);
      context.CourtMessageIds = context.CourtMessageIds.Append(response.Id).ToArray();
    }
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
      var messageText = $"Faltan {this.paddleConfig.PlayerCount - count} votos";
      await this.botClient.EditMessageText(context.ChatId!, context.CountMessageId, messageText);
      return true;
    }
    else
    {
      await this.SendAvailability(context);
      return true;
    }
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

  public async Task<bool> SendAvailableDates(Update update)
  {
    this.contextService.SetChatContext(update);
    var chatId = update?.Message?.Chat.Id;
    var threadId = update?.Message?.MessageThreadId;
    var command = update?.Message?.Text ?? "";
    var dateRange = DateTime.Today.AddDays(this.paddleConfig.DaysInAdvance);
    var forecast = this.mapper.Map<WeatherForecast[]>(await this.weatherService.GetWeatherForecast());

    var context = this.contextService.GetChatContext(update!);
    if (context.LatestDayPicker != default)
    {
      await this.botClient.DeleteMessage(context.ChatId, context.LatestDayPicker);
      context.LatestDayPicker = default;
    }

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
    this.logger.LogInformation("ChatId: {ChatId}", chatId);
    this.logger.LogInformation("Context: {Context}", context);

    var message = await this.botClient.SendMessage(chatId, "Elegi dia", messageThreadId: context.MessageThreadId, replyMarkup: inlineKeyboard, disableNotification: true);
    context.LatestDayPicker = message.MessageId;
    return true;
  }

  private async Task OnDateSelected(Update update, string date)
  {
    var chatId = update?.CallbackQuery?.Message?.Chat.Id;
    var threadId = update?.CallbackQuery?.Message?.MessageThreadId;

    var matchDate = date;

    if (chatId == null)
    {
      this.logger.LogWarning("Chat ID null for update {Id} on ReadyCheckPoll step", update.Id);
    }

    if (matchDate == null)
    {
      this.logger.LogWarning("Match Date returned null from Update Id {Id}", update.Id);
    }

    var context = this.contextService.GetChatContext(update);
    context.SelectedDate = matchDate;
    if (context.LatestDayPicker > 0)
    {
      if (this.telegramConfig.DeleteMessages)
      {
        await this.botClient.DeleteMessage(context.ChatId, context.LatestDayPicker);
      }
      context.LatestDayPicker = 0;
    }
  }

  private async Task<bool> StartReadyCheckPoll(UpdateContext context)
  {
    if (context.LatestPollId > 0)
    {
      await this.botClient.DeleteMessage(context.ChatId, context.LatestPollId);
      context.LatestPollId = 0;
    }

    var poll = await this.botClient.SendPoll(context.ChatId, $"Estas para jugar el {context.SelectedDate}?", options, isAnonymous: false, messageThreadId: context.MessageThreadId, disableNotification: true);
    context.LatestPollId = poll.MessageId;
    Common.pollChatIdDict.Add(poll.Poll.Id, context.ChatId);
    await SendPlayerCountMessage(context);
    return true;
  }

  private async Task SendPlayerCountMessage(UpdateContext context, string messageText = "Faltan 4 votos")
  {
    var countMessage = await this.botClient.SendMessage(context.ChatId, messageText, messageThreadId: context.MessageThreadId, disableNotification: true);
    context.CountMessageId = countMessage.MessageId;
  }

  public async Task<bool> HandleCourtPick(Update update)
  {
    var context = this.contextService.GetChatContext(update);
    await this.botClient.DeleteMessages(context.ChatId, context.CourtMessageIds);
    context.CourtMessageIds = Array.Empty<int>();
    (var _, var selection) = (update?.CallbackQuery?.Data).DecodeCallback();
    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    (var clubId, var courtId) = selection.SplitBy(":");
    var club = availability.Clubs.FirstOrDefault(c => c.Id == clubId);
    var court = club.Courts.FirstOrDefault(c => c.Id == courtId);

    var buttons = court.Availability.Select(
        a =>
        {
          var value = $"{clubId}+{courtId}+{a.Start}+{a.Duration}";
          return new[]
                  {
                    InlineKeyboardButton.WithCallbackData($"{a.Start} {a.Duration}min ${a.Price}", (Common.PICK_HOUR_COMMAND, value).EncodeCallback()),
          };
        }).ToArray();

    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons);
    var response = await this.botClient.SendMessage(
        context.ChatId,
        $"Reservar {club.Name} {court.Name} {context.SelectedDate}",
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard
        );
    context.HourPickerId = response.Id;
    return true;
  }

  public async Task<bool> HandleHourPick(Update update)
  {
    var context = this.contextService.GetChatContext(update);
    await this.botClient.DeleteMessage(context.ChatId, context.HourPickerId);
    context.HourPickerId = default;
    (var _, var selection) = (update?.CallbackQuery?.Data).DecodeCallback();

    (var clubId, var courtId, var start, var duration) = selection.SplitBy4("+");

    return await SendReservationActions(context, clubId, courtId, start, duration);
  }

  public async Task<bool> SendReservationActions(UpdateContext context, string clubId, string courtId, string start, string duration, string clubName = "", string courtName = "")
  {
    string callbackValue = $"{clubId}+{courtId}+{start}+{duration}";

    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup();
    var url = this.paddleService.GetCheckoutUrl(clubId, context.SelectedDate, courtId, start, duration);
    inlineKeyboard.AddNewRow(InlineKeyboardButton.WithUrl("Reservar", url));
    inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("Pinear mensaje", (Common.PIN_REMINDER_COMMAND, callbackValue).EncodeCallback()));

    var response = await this.botClient.SendMessage(
        context.ChatId,
        text: @$"Reservar {start} {duration}min {context.SelectedDate} 
{clubName}
{courtName}
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
    (var _, var selection) = (update?.CallbackQuery?.Data).DecodeCallback();
    await OnDateSelected(update, selection);
    if (context.LastCommand.Contains(this.telegramConfig.Commands.ReadyCheck))
    {
      return await StartReadyCheckPoll(context);
    }
    else if (context.LastCommand.Contains(this.telegramConfig.Commands.Search))
    {
      return await SendAvailability(context);
    }
    return false;
  }

  public async Task<bool> SendPinnedMatchReminderMessage(Update update)
  {
    var context = this.contextService.GetChatContext(update);
    (var _, var selection) = (update?.CallbackQuery?.Data).DecodeCallback();

    (var clubId, var courtId, var start, var duration) = selection.SplitBy4("+");
    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    var club = availability.Clubs.FirstOrDefault(c => c.Id == clubId);
    var court = club.Courts.FirstOrDefault(c => c.Id == courtId);

    var message = @$"Detalles del partido:
🏟️ {club.Name} - 📍 {club.Location.Address} - {context.SelectedDate}
{court.Name} - {start} {duration}min
        ";

    var response = await this.botClient.SendMessage(
        context.ChatId,
        message,
        messageThreadId: context.MessageThreadId,
        disableNotification: true);
    await this.botClient.PinChatMessage(context.ChatId, response.MessageId, disableNotification: true);
    return true;
  }
}
