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

public class PollHandler : IUpdateHandler
{
  ITelegramBotClient _botClient;
  ILogger<PollHandler> _logger;
  PaddleServiceConfiguration _paddleConfig;
  TelegramConfiguration _telegramConfig;
  IPaddleService _paddleService;
  IContextService _contextService;
  IWeatherService _weatherService;
  IMapper _mapper;

  public PollHandler(
      ITelegramBotClient botClient,
      IPaddleService paddleService,
      IWeatherService weatherService,
      IContextService contextService,
      ILogger<PollHandler> logger,
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

  public async Task<bool> Handle(Update update)
  {
    var count = update?.Poll?.Options.Single(o => o.Text == "Si").VoterCount;
    _logger.LogInformation("Handling poll");
    if (!Common.pollChatIdDict.TryGetValue(update.Poll.Id, out var chatId))
    {
      _logger.LogWarning("Poll ID {Id} not found", update.Poll.Id);
      return false;
    }
    var context = _contextService.GetChatContext(chatId);

    if (count < _paddleConfig.PlayerCount)
    {
      var messageText = $"Faltan {_paddleConfig.PlayerCount - count} votos";
      await _botClient.EditMessageText(context.ChatId, context.CountMessageId, messageText);
      return true;
    }
    else
    {
      await Search(context);
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
                InlineKeyboardButton.WithCallbackData($"{club.Name} - {c.Name} ", Common.EncodeCallback(Common.PICK_COURT_COMMAND, value)),
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
}
