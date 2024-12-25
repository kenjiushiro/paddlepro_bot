using Telegram.Bot.Types;

namespace paddlepro.API.Services;

public class UpdateDispatcher
{

  private readonly HandlerResolver handlerResolver;

  public UpdateDispatcher(HandlerResolver handlerResolver)
  {
    this.handlerResolver = handlerResolver;
  }

  public async Task<bool> Dispatch(Update update)
  {
    var handler = handlerResolver(update.Type);

    if (handler != default)
    {
      return await handler.Handle(update);
    }
    return false;
  }
}
