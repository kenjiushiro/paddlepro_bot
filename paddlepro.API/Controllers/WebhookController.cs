using Microsoft.AspNetCore.Mvc;
using paddlepro.API.Services.Interfaces;

namespace paddlepro.API.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
  private readonly ILogger<WebhookController> _logger;
  private readonly IWeatherService _weatherService;
  private readonly ITelegramService _telegramService;

  public WebhookController(
      ILogger<WebhookController> logger,
      IWeatherService weatherService,
      ITelegramService telegramService
      )
  {
    _logger = logger;
    _weatherService = weatherService;
    _telegramService = telegramService;
  }

  [HttpPost]
  public async Task<bool> Post([FromBody] Telegram.Bot.Types.Update update)
  {
    _logger.LogInformation("Received update with ID {ID} of Type {Type}", update.Id, update.Type);
    return await _telegramService.HandleWebhook(update);
  }
}
