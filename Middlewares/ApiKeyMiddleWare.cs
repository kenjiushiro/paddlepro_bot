using Microsoft.Extensions.Options;
using paddlepro.API.Configurations;

namespace paddlepro.API.Middlewares;

public class ApiKeyValidationMiddleware
{
  private readonly RequestDelegate next;
  private readonly ILogger<ApiKeyValidationMiddleware> logger;
  private readonly string expectedApiKey;

  public ApiKeyValidationMiddleware(RequestDelegate next, IOptions<TelegramConfiguration> config, ILogger<ApiKeyValidationMiddleware> logger)
  {
    this.next = next;
    this.logger = logger;
    this.expectedApiKey = config.Value.ApiKey;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    if (!context.Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var providedApiKey))
    {
      this.logger.LogWarning("Missing API Key");
      context.Response.StatusCode = StatusCodes.Status401Unauthorized;
      await context.Response.WriteAsync("API Key is missing.");
      return;
    }

    // Compare the provided key with the expected key
    if (!this.expectedApiKey.Equals(providedApiKey))
    {
      this.logger.LogWarning("Invalid API Key");
      context.Response.StatusCode = StatusCodes.Status403Forbidden;
      await context.Response.WriteAsync("Invalid API Key.");
      return;
    }

    // Continue to the next middleware or action
    await this.next(context);
  }
}
