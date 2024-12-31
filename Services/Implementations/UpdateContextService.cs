using Microsoft.Extensions.Caching.Memory;
using paddlepro.API.Models;
using paddlepro.API.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace paddlepro.API.Services.Implementations;

public class UpdateContextService : IUpdateContextService
{
  private readonly IMemoryCache cache;
  private readonly ILogger<UpdateContextService> logger;

  public UpdateContextService(
      IMemoryCache cache,
      ILogger<UpdateContextService> logger
      )
  {
    this.cache = cache;
    this.logger = logger;
  }

  private UpdateContext SetChatContext(Update update)
  {
    var chatId = update?.Message?.Chat.Id!;
    var messageThreadId = update?.Message?.MessageThreadId!;
    var lastCommand = update?.Message?.Text ?? "";

    var context = new UpdateContext
    {
      ChatId = chatId,
      MessageThreadId = messageThreadId,
      NextStep = lastCommand,
    };
    this.cache.Set(chatId, context);
    return context;
  }

  public long? GetChatId(Update update)
  {
    switch (update.Type)
    {
      case UpdateType.Message:
        return update?.Message?.Chat?.Id;
      case UpdateType.CallbackQuery:
        return update?.CallbackQuery?.Message?.Chat?.Id;
      case UpdateType.Poll:
        return Common.pollChatIdDict[update?.Poll?.Id!];
      default:
        throw new Exception($"Can't get an ID from an update type {update.Type}");
    }
  }

  public UpdateContext GetChatContext(Update update)
  {
    var chatId = GetChatId(update);
    var context = this.cache.Get<UpdateContext>(chatId!);
    return context ?? SetChatContext(update);
  }
}
