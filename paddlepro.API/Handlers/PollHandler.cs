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
  ITelegramBotClient botClient;
  ILogger<PollHandler> logger;
  AtcServiceConfiguration paddleConfig;
  IPaddleService paddleService;
  IContextService contextService;
  IMapper mapper;

  public PollHandler(
      ITelegramBotClient botClient,
      IPaddleService paddleService,
      IContextService contextService,
      ILogger<PollHandler> logger,
      IOptions<AtcServiceConfiguration> paddleConfig,
      IMapper mapper
      )
  {
    this.botClient = botClient;
    this.logger = logger;
    this.contextService = contextService;
    this.paddleService = paddleService;
    this.paddleConfig = paddleConfig.Value;
    this.mapper = mapper;
  }

  public async Task<bool> Handle(Update update)
  {
    var count = update?.Poll?.Options.Single(o => o.Text == "Si").VoterCount;
    this.logger.LogInformation("Handling poll");
    if (!Common.pollChatIdDict.TryGetValue(update.Poll.Id, out var chatId))
    {
      this.logger.LogWarning("Poll ID {Id} not found", update.Poll.Id);
      return false;
    }
    var context = this.contextService.GetChatContext(chatId);

    if (count < this.paddleConfig.PlayerCount)
    {
      var messageText = $"Faltan {this.paddleConfig.PlayerCount - count} votos";
      await this.botClient.EditMessageText(context.ChatId, context.CountMessageId, messageText);
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

  }
}
