using Microsoft.Extensions.Diagnostics.HealthChecks;
using Telegram.Bot;

namespace paddlepro.API.HealthCheck;

public class TelegramServiceHealthCheck : IHealthCheck
{
    private readonly ITelegramBotClient bot;

    public TelegramServiceHealthCheck(ITelegramBotClient bot)
    {
        this.bot = bot;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await bot.GetMe();
            var data = new Dictionary<string, object>
            {
                { "Endpoint", "getMe" },
                { "StatusCode", 200 }
            };

            return HealthCheckResult.Healthy("OK", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Telegram bot health check failed", ex);
        }
    }
}
