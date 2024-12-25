using Telegram.Bot.Types;

namespace paddlepro.API.Services;

public class UpdateDispatcher
{

  private readonly HandlerResolver handlerResolver;
  private readonly ILogger<UpdateDispatcher> logger;

  public UpdateDispatcher(
      HandlerResolver handlerResolver,
      ILogger<UpdateDispatcher> logger
      )
  {
    this.handlerResolver = handlerResolver;
    this.logger = logger;
  }

  public async Task<bool> Dispatch(Update update)
  {
    var handler = handlerResolver(update.Type);

    if (handler == default)
    {
      this.logger.LogWarning("Handler not found for type {Type}", update.Type.ToString());
      return false;
    }
    this.logger.LogInformation("Handler {Handler} found for type {Type}", handler, update.Type.ToString());
    return await handler.Handle(update);
  }
}
