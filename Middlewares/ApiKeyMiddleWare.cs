namespace paddlepro.API.Middlewares;

public class ApiKeyValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _expectedApiKey;

    public ApiKeyValidationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        // Retrieve the expected API key from configuration
        _expectedApiKey = configuration["Telegram:ApiKey"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the header is present
        if (!context.Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var providedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key is missing.");
            return;
        }

        // Compare the provided key with the expected key
        if (!_expectedApiKey.Equals(providedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Invalid API Key.");
            return;
        }

        // Continue to the next middleware or action
        await _next(context);
    }
}
