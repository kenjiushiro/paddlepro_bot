using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using paddlepro.API.Configurations;
using paddlepro.API.Services;
using paddlepro.API.Services.Interfaces;
using paddlepro.API.Models.Application;
using AutoMapper;

namespace paddlepro.API.Handlers;

public class MessageHandler : IUpdateHandler
{
  ITelegramBotClient botClient;
  ILogger<MessageHandler> logger;
  AtcServiceConfiguration paddleConfig;
  TelegramConfiguration telegramConfig;
  IContextService contextService;
  IWeatherService weatherService;
  IMapper mapper;

  public MessageHandler(
      ITelegramBotClient botClient,
      IWeatherService weatherService,
      IContextService contextService,
      ILogger<MessageHandler> logger,
      IOptions<AtcServiceConfiguration> paddleConfig,
      IOptions<TelegramConfiguration> telegramConfig,
      IMapper mapper
      )
  {
    this.botClient = botClient;
    this.logger = logger;
    this.contextService = contextService;
    this.weatherService = weatherService;
    this.paddleConfig = paddleConfig.Value;
    this.telegramConfig = telegramConfig.Value;
    this.mapper = mapper;
  }

  public async Task<bool> Handle(Update update)
  {
    var chatId = update?.Message?.Chat.Id;
    var threadId = update?.Message?.MessageThreadId;
    var command = update?.Message?.Text ?? "";
    this.logger.LogInformation("Handling message: {Message}", command);

    if (!command.Contains(this.telegramConfig.Commands.ReadyCheck) && !command.Contains(this.telegramConfig.Commands.Search))
    {
      return false;
    }
    this.logger.LogInformation("Command: {Command}", command);
    this.contextService.SetChatContext(chatId, threadId, "", command);
    await SendAvailableDates(chatId);
    return true;
  }


  private async Task SendAvailableDates(long? chatId)
  {
    var dateRange = DateTime.Today.AddDays(this.paddleConfig.DaysInAdvance);
    var forecast = this.mapper.Map<WeatherForecast[]>(await this.weatherService.GetWeatherForecast());

    var context = this.contextService.GetChatContext(chatId);

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
      inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData(buttonDisplay, Common.EncodeCallback(Common.PICK_DATE_COMMAND, buttonValue)));
    }

    var message = await this.botClient.SendMessage(chatId, "Elegi dia", messageThreadId: context.MessageThreadId, replyMarkup: inlineKeyboard, disableNotification: true);
    context.LatestDayPicker = message.MessageId;
  }
}

