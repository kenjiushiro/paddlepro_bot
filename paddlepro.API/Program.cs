using Telegram.Bot;
using paddlepro.API.Services;
using System.Text.Json;
using paddlepro.API.Configurations;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
  options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
  options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
  options.JsonSerializerOptions.WriteIndented = true;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<WeatherServiceConfiguration>(builder.Configuration.GetSection("WeatherService"));
builder.Services.Configure<TelegramConfiguration>(builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<PaddleServiceConfiguration>(builder.Configuration.GetSection("PaddleService"));

builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IPaddleService, AtcService>();
builder.Services.AddScoped<ITelegramService, TelegramService>();

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
  var botToken = builder.Configuration["BotConfiguration:BotToken"];
  // TODO this might have a cleanr way to instantiate, read docu
  return new TelegramBotClient(botToken);
});

var app = builder.Build();

// TODO re enable this?
/*app.UseHttpsRedirection();*/

app.UseAuthorization();

app.MapControllers();

app.Run();
