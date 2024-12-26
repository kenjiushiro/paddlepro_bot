using paddlepro.API.Models;
using paddlepro.API.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace paddlepro.API.Services.Implementations;

public class UpdateContextService : IUpdateContextService
{
  private readonly List<UpdateContext> contexts;
  public UpdateContextService()
  {
    this.contexts = new List<UpdateContext>();
  }

  public void SetChatContext(Update update)
  {
    var chatId = update?.Message?.Chat.Id;
    var messageThreadId = update?.Message?.MessageThreadId;
    var lastCommand = update?.Message?.Text ?? "";

    var context = GetChatContext(update);
    if (context != null)
    {
      context.MessageThreadId = messageThreadId;
      context.LastCommand = lastCommand;
    }
    else
    {
      this.contexts.Add(new UpdateContext
      {
        ChatId = chatId,
        MessageThreadId = messageThreadId,
        LastCommand = lastCommand,
      });

    }
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
        return Common.pollChatIdDict[update?.Poll?.Id];
      default:
        throw new Exception("TBD");
    }
  }

  public UpdateContext GetChatContext(Update update)
  {
    var chatId = GetChatId(update);
    return this.contexts.FirstOrDefault(c => c.ChatId == chatId);
  }
}
