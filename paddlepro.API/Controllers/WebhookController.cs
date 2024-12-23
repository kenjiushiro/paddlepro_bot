using Telegram.Bot;
using Microsoft.AspNetCore.Mvc;
using paddlepro.API.Models;
using paddlepro.API.Services;

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
    public bool Post([FromBody] Telegram.Bot.Types.Update update)
    {
        _telegramService.Respond(update);
        return true;
    }

    [HttpGet("availability")]
    public WeatherForecast Get([FromQuery] DateTime date)
    {
        return this._weatherService.GetWeatherForecast(date);
    }
}
