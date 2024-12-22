using Telegram.Bot.Types;
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

    public WebhookController(
        ILogger<WebhookController> logger,
        IWeatherService weatherService
        )
    {
        _logger = logger;
        _weatherService = weatherService;
    }

    [HttpPost]
    public Update Post([FromBody] Update update)
    {
        _logger.LogInformation("Id: {UpdateId}", update.Id);
        _logger.LogInformation("Text: {Text}", update.Message?.Text);
        return update;
    }

    [HttpGet("availability")]
    public WeatherForecast Get([FromQuery] DateTime date)
    {
        return this._weatherService.GetWeatherForecast(date);
    }
}
