using Telegram.Bot.Types;
using paddlepro.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using paddlepro.API.Configurations;

namespace paddlepro.API.Handlers;

public class MessageHandler : IUpdateHandler
{
  private readonly ILogger<MessageHandler> logger;
  private readonly ITelegramService telegramService;
  private readonly TelegramConfiguration telegramConfig;

  public MessageHandler(
      ILogger<MessageHandler> logger,
      IOptions<TelegramConfiguration> telegramConfig,
      ITelegramService telegramService
      )
  {
    this.logger = logger;
    this.telegramService = telegramService;
    this.telegramConfig = telegramConfig.Value;
  }

  public async Task<bool> Handle(Update update)
  {
    var command = update?.Message?.Text ?? "";

    this.logger.LogInformation("Command: {Command}", command);

    if (command.Contains(this.telegramConfig.Commands.ReadyCheck))
    {
      return await this.telegramService.SendAvailableDates(update!, "readyCheck");
    }
    else if (command.Contains(this.telegramConfig.Commands.Search))
    {
      return await this.telegramService.SendAvailableDates(update!, "search");
    }
    else if (command.Contains(this.telegramConfig.Commands.BookCourt))
    {
      return await this.telegramService.BookCourt(update!);
    }
    return false;
  }

}

