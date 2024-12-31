using Telegram.Bot;
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
  private readonly IMessageFormatterService messageFormatter;
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
      IMessageFormatterService messageFormatter,
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
    this.messageFormatter = messageFormatter;
  }

  public static DateTime ParseDate(string input)
  {
    var today = DateTime.Today;

    DateTime GetNextDayOccurrence(DayOfWeek dayOfWeek)
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

    return true;
  }

  public async Task<bool> SendAvailableClubs(UpdateContext context)
  {
    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    var clubs = availability.Clubs.Where(x => this.paddleConfig.ClubIds.ToList().Contains(x.Id) && x.IsAvailable).ToArray();

    var (message, inlineKeyboard) = this.messageFormatter.FormatClubsAvailabilityMessage(clubs, context.SelectedDate);

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

  private async Task DeleteMessages(UpdateContext context, BotMessageType type)
  {
    if (this.telegramConfig.DeleteMessages && context.Messages.Any())
    {
      var messageToDelete = context.Messages.Select(m => m.Id);
      await this.botClient.DeleteMessages(context.ChatId!, messageToDelete);
      context.ClearMessages();
    }
  }

  public async Task<bool> SendDatePicker(Update update, string nextStep)
  {
    var context = this.contextService.GetChatContext(update);
    context.NextStep = nextStep;
    var dateRange = DateTime.Today.AddDays(this.paddleConfig.DaysInAdvance);
    var forecast = await this.weatherService.GetWeatherForecast();

    await this.DeleteMessages(context, BotMessageType.DayPicker);

    (var message, var inlineKeyboard) = this.messageFormatter.FormatDayPicker(forecast, this.paddleConfig.DaysInAdvance);

    var response = await this.botClient.SendMessage(context.ChatId!, "Elegi dia", messageThreadId: context.MessageThreadId, replyMarkup: inlineKeyboard, disableNotification: true);
    context.AddMessage(response.MessageId, BotMessageType.DayPicker);
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

    private async Task DeleteMessages(UpdateContext context, BotMessageType type)
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

  public async Task<bool> HandleCourtPick(Update update, string callbackData)
  {
    var context = this.contextService.GetChatContext(update);

    await this.DeleteMessages(context, BotMessageType.CourtMessage);

    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    (var clubId, var courtId) = callbackData.SplitBy(":");
    var club = availability.Clubs.FirstOrDefault(c => c.Id == clubId);
    var court = club?.Courts.FirstOrDefault(c => c.Id == courtId);

    (var message, var inlineKeyboard) = this.messageFormatter.FormatMatchPickerMessage(club!, court!, context.SelectedDate);

    var response = await this.botClient.SendMessage(
        context.ChatId!,
        message,
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2
        );
    context.AddMessage(response.Id, BotMessageType.HourPicker);
    return true;
  }

  public async Task<bool> HandleClubPick(Update update, string callbackData)
  {
    var context = this.contextService.GetChatContext(update);

    await this.DeleteMessages(context, BotMessageType.ClubMessage);

    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    var club = availability.Clubs.FirstOrDefault(c => c.Id == callbackData);

    (var message, var inlineKeyboard) = this.messageFormatter.FormatCourtsAvailabilityMessage(club!, context.SelectedDate);

    var response = await this.botClient.SendMessage(
        context.ChatId!,
        message,
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2
        );
    context.AddMessage(response.Id, BotMessageType.CourtMessage);
    return true;
  }

  public async Task<bool> HandleHourPick(Update update, string callbackData)
  {
    var context = this.contextService.GetChatContext(update);
    (var clubId, var courtId, var start, var duration) = callbackData.SplitBy4("+");
    return await SendReservationActions(context, clubId, courtId, start, duration);
  }

  public async Task<bool> SendReservationActions(UpdateContext context, string clubId, string courtId, string start, string duration)
  {
    await this.DeleteMessages(context, BotMessageType.HourPicker);

    var club = this.paddleService.GetClubDetails(clubId);
    var court = club.Courts.FirstOrDefault(c => c.Id == courtId);

    var url = this.paddleService.GetCheckoutUrl(club.Id, context.SelectedDate, court?.Id!, start, duration);

    var (message, inlineKeyboard) = this.messageFormatter.FormatSendReservationActions(club, court!, context.SelectedDate, start, duration, url);

    var response = await this.botClient.SendMessage(
        context.ChatId!,
        text: message,
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard
        );
    return true;
  }

  public async Task<bool> HandleDatePick(Update update, string callbackData)
  {
    var context = this.contextService.GetChatContext(update);
    await this.DeleteMessages(context, BotMessageType.DayPicker);

    await OnDateSelected(update!, callbackData);
    this.logger.LogInformation("Selected date: {SelectedDate} Next step: {NextStep}", callbackData, context.NextStep);

    if (context.NextStep == "readyCheck")
    {
      return await StartReadyCheckPoll(context);
    }

    public async Task<bool> HandleDatePick(Update update)
    {
      return await SendAvailableClubs(context);
    }

  public async Task<bool> HandlerPinReminderPick(Update update, string callbackData)
  {
    var context = this.contextService.GetChatContext(update);

    (var clubId, var courtId, var start, var duration) = callbackData.SplitBy4("+");
    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    var club = availability.Clubs.FirstOrDefault(c => c.Id == clubId);
    var court = club?.Courts.FirstOrDefault(c => c.Id == courtId);

    var message = this.messageFormatter.FormatPinnedMessageReminder(club!, court!, context.SelectedDate, start, duration);

    var response = await this.botClient.SendMessage(
        context.ChatId!,
        message,
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2
        );
    await this.botClient.PinChatMessage(context.ChatId!, response.MessageId, disableNotification: true);
    return true;
  }
}
