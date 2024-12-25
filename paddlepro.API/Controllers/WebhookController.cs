using Microsoft.AspNetCore.Mvc;
using paddlepro.API.Services;

namespace paddlepro.API.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
  private readonly ILogger<WebhookController> logger;
  private readonly UpdateDispatcher updateDispatcher;

  public WebhookController(
      ILogger<WebhookController> logger,
      UpdateDispatcher updateDispatcher
      )
  {
    this.logger = logger;
    this.updateDispatcher = updateDispatcher;
  }

  [HttpPost]
  public async Task<bool> Post([FromBody] Telegram.Bot.Types.Update update)
  {
    this.logger.LogInformation("Received update with ID {ID} of Type {Type}", update.Id, update.Type);
    return await this.updateDispatcher.Dispatch(update);
  }
}
