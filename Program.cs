using paddlepro.API.Profiles;
using paddlepro.API.Handlers;
using paddlepro.API.Bootstrap;
using System.Text.Json;
using paddlepro.API.HealthCheck;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using paddlepro.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
  options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
  options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
  options.JsonSerializerOptions.WriteIndented = true;
});

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddUserSecrets<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks()
    .AddCheck<AtcHealthCheck>("AtcHealthCheck")
    .AddCheck<WeatherServiceHealthCheck>("WeatherServiceHealthCheck")
    .AddCheck<TelegramServiceHealthCheck>("TelegramServiceHealthCheck");

builder.Services.AddAutoMapper(typeof(WeatherProfile));

builder.Services.AddServices(builder.Configuration);
builder.Services.AddConfiguration(builder.Configuration);
builder.Services.AddHandlers();


var app = builder.Build();

// TODO re enable this?
/*app.UseHttpsRedirection();*/

app.UseAuthorization();
app.UseMiddleware<ApiKeyValidationMiddleware>();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
  ResponseWriter = HealthCheckWriter.ResponseWriter
});

app.Run();

public delegate IUpdateHandler HandlerResolver(Telegram.Bot.Types.Enums.UpdateType type);
