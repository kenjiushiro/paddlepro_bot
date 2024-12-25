using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using paddlepro.API.Configurations;
using paddlepro.API.Models;
using paddlepro.API.Services;
using paddlepro.API.Helpers;
using paddlepro.API.Services.Interfaces;
using paddlepro.API.Models.Application;
using AutoMapper;

namespace paddlepro.API.Handlers;

public class CallbackQueryHandler : IUpdateHandler
{
  private readonly ILogger<CallbackQueryHandler> logger;
  private readonly IContextService contextService;
  private readonly ITelegramBotClient botClient;
  private readonly AtcServiceConfiguration paddleConfig;
  private readonly TelegramConfiguration telegramConfig;
  private readonly IPaddleService paddleService;
  private readonly IMapper mapper;

  private List<InputPollOption> options = new List<InputPollOption> { new InputPollOption("Si"), new InputPollOption("No") };


  public CallbackQueryHandler(
      ITelegramBotClient botClient,
      IPaddleService paddleService,
      IContextService contextService,
      ILogger<CallbackQueryHandler> logger,
      IOptions<AtcServiceConfiguration> paddleConfig,
      IOptions<TelegramConfiguration> telegramConfig,
      IMapper mapper
      )
  {
    this.botClient = botClient;
    this.logger = logger;
    this.contextService = contextService;
    this.paddleService = paddleService;
    this.paddleConfig = paddleConfig.Value;
    this.telegramConfig = telegramConfig.Value;
    this.mapper = mapper;
  }


  public async Task<bool> Handle(Update update)
  {
    Dictionary<string, Func<Update, Context, string, Task<bool>>> commands = new Dictionary<string, Func<Update, Context, string, Task<bool>>>
    {
      { Common.PICK_DATE_COMMAND, HandleDatePick },
      { Common.PICK_COURT_COMMAND, HandleCourtPick },
      { Common.PICK_HOUR_COMMAND, HandleHourPick },
      { Common.SCHEDULE_REMINDER_COMMAND, ScheduleReminder },
    };

    var chatId = update?.CallbackQuery?.Message?.Chat.Id;
    var context = this.contextService.GetChatContext(chatId);
    (var action, var callbackValue) = Common.DecodeCallback(update?.CallbackQuery?.Data);

    if (!commands.TryGetValue(action, out var actionHandler))
    {
      this.logger.LogWarning("Couldn'f find callback query action {Action}}", action);
      return false;
    }

    this.logger.LogInformation("Handling callback query: {Action}", action);
    return await actionHandler(update, context, callbackValue);
  }

  private async Task<bool> ScheduleReminder(Update update, Context context, string selection)
  {
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

    var context = this.contextService.GetChatContext(chatId);
    context.SelectedDate = matchDate;
    if (context.LatestDayPicker > 0)
    {
      /*this.botClient.DeleteMessage(context.ChatId, context.LatestDayPicker);*/
      context.LatestDayPicker = 0;
    }
  }


  private async Task<bool> StartReadyCheckPoll(long? chatId)
  {
    var context = this.contextService.GetChatContext(chatId);
    if (context.LatestPollId > 0)
    {
      this.botClient.DeleteMessage(context.ChatId, context.LatestPollId);
      context.LatestPollId = 0;
    }

    var poll = await this.botClient.SendPoll(context.ChatId, $"Estas para jugar el {context.SelectedDate}?", options, isAnonymous: false, messageThreadId: context.MessageThreadId, disableNotification: true);
    context.LatestPollId = poll.MessageId;
    Common.pollChatIdDict.Add(poll.Poll.Id, chatId);
    await SendPlayerCountMessage(context);
    return true;
  }

  private async Task<bool> HandleDatePick(Update update, Context context, string selection)
  {
    await OnDateSelected(update, selection);
    if (context.LastCommand.Contains(this.telegramConfig.Commands.ReadyCheck))
    {
      return await StartReadyCheckPoll(context.ChatId);
    }
    else if (context.LastCommand.Contains(this.telegramConfig.Commands.Search))
    {
      return await Search(context);
    }
    return false;
  }

  private async Task<bool> HandleCourtPick(Update update, Context context, string selection)
  {
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
                    InlineKeyboardButton.WithCallbackData($"{a.Start} {a.Duration}min ${a.Price}", Common.EncodeCallback(Common.PICK_HOUR_COMMAND, value)),
          };
        }).ToArray();

    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons);
    var response = await this.botClient.SendMessage(
        context.ChatId,
        $"Reservar {club.Name} {court.Name}",
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard
        );
    return true;
  }

  private async Task<bool> HandleHourPick(Update update, Context context, string selection)
  {
    (var clubId, var courtId, var start, var duration) = selection.SplitBy4("+");

    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup();
    var url = this.paddleService.GetCheckoutUrl(clubId, context.SelectedDate, courtId, start, duration);
    inlineKeyboard.AddNewRow(InlineKeyboardButton.WithUrl("Reservar", url));
    inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("Activar recordatorio", Common.EncodeCallback(Common.SCHEDULE_REMINDER_COMMAND, selection)));

    var response = await this.botClient.SendMessage(
        context.ChatId,
        text: $"Reservar {start} {duration}min",
        messageThreadId: context.MessageThreadId,
        disableNotification: true,
        replyMarkup: inlineKeyboard
        );
    return true;
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

  private async Task<bool> Search(Context context)
  {
    var availability = this.mapper.Map<Availability>(await this.paddleService.GetAvailability(context.SelectedDate));
    var clubs = availability.Clubs.Where(x => this.paddleConfig.ClubIds.ToList().Contains(x.Id)).ToArray();

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
                InlineKeyboardButton.WithCallbackData($"{club.Name} - {c.Name} ", Common.EncodeCallback(Common.PICK_COURT_COMMAND, value)),
      };

          }
          ).ToArray();
      InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons);
      var response = await this.botClient.SendMessage(
          context.ChatId,
          message,
          messageThreadId: context.MessageThreadId,
          disableNotification: true,
          replyMarkup: inlineKeyboard,
          parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2);
    }
    return true;
  }

  private async Task SendPlayerCountMessage(Context context, string messageText = "Faltan 4 votos")
  {
    var countMessage = await this.botClient.SendMessage(context.ChatId, messageText, messageThreadId: context.MessageThreadId, disableNotification: true);
    context.CountMessageId = countMessage.MessageId;
  }
}
