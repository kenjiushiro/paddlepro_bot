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
  ITelegramBotClient _botClient;
  ILogger<MessageHandler> _logger;
  PaddleServiceConfiguration _paddleConfig;
  TelegramConfiguration _telegramConfig;
  IPaddleService _paddleService;
  IContextService _contextService;
  IWeatherService _weatherService;
  IMapper _mapper;

  public MessageHandler(
      ITelegramBotClient botClient,
      IPaddleService paddleService,
      IWeatherService weatherService,
      IContextService contextService,
      ILogger<MessageHandler> logger,
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
    var chatId = update?.Message?.Chat.Id;
    var threadId = update?.Message?.MessageThreadId;
    var command = update?.Message?.Text ?? "";
    _logger.LogInformation("Handling message: {Message}", command);

    if (!command.Contains(_telegramConfig.Commands.ReadyCheck) && !command.Contains(_telegramConfig.Commands.Search))
    {
      return false;
    }
    _logger.LogInformation("Command: {Command}", command);
    _contextService.SetChatContext(chatId, threadId, "", command);
    await SendAvailableDates(chatId);
    return true;
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
      inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData(buttonDisplay, Common.EncodeCallback(Common.PICK_DATE_COMMAND, buttonValue)));
    }

    var message = await _botClient.SendMessage(chatId, "Elegi dia", messageThreadId: context.MessageThreadId, replyMarkup: inlineKeyboard, disableNotification: true);
    context.LatestDayPicker = message.MessageId;
  }
}

