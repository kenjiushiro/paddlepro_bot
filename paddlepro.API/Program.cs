using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using paddlepro.API.Services.Implementations;
using paddlepro.API.Services.Interfaces;
using paddlepro.API.Profiles;
using paddlepro.API.Handlers;
using System.Text.Json;
using paddlepro.API.Configurations;
using paddlepro.API.Services;

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

builder.Services.AddAutoMapper(typeof(WeatherProfile));

builder.Services.Configure<WeatherServiceConfiguration>(builder.Configuration.GetSection("WeatherService"));
builder.Services.Configure<TelegramConfiguration>(builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<PaddleServiceConfiguration>(builder.Configuration.GetSection("PaddleService"));

builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IPaddleService, AtcService>();

builder.Services.AddScoped<IUpdateHandler, MessageHandler>();
builder.Services.AddScoped<IUpdateHandler, CallbackQueryHandler>();
builder.Services.AddScoped<IUpdateHandler, PollHandler>();

builder.Services.AddScoped<UpdateDispatcher>();

builder.Services.AddTransient<HandlerResolver>(serviceProvider => key =>
{
  switch (key)
  {
    case UpdateType.Message:
      return serviceProvider.GetService<MessageHandler>();
    case UpdateType.CallbackQuery:
      return serviceProvider.GetService<CallbackQueryHandler>();
    case UpdateType.Poll:
      return serviceProvider.GetService<PollHandler>();
    default:
      throw new KeyNotFoundException();
  }
});

builder.Services.AddSingleton<IContextService, ContextService>();

builder.Services.AddHttpClient<IWeatherService, WeatherService>(client =>
{
  var baseUrl = builder.Configuration["WeatherService:BaseUrl"];
  var apiKey = builder.Configuration["WeatherService:ApiKey"];
  var days = builder.Configuration["PaddleService:DaysInAdvance"];
  var baseUri = new Uri(baseUrl);

  // Add query parameters
  var queryParams = new Dictionary<string, string>
    {
        { "q", "Buenos%20Aires" },
        { "key", apiKey }
    };
  client.BaseAddress = new Uri($"https://api.weatherapi.com/v1/forecast.json?q=Buenos%20Aires&key={apiKey}&days={days}");
});

builder.Services.AddHttpClient<IPaddleService, AtcService>(client =>
{
  var baseUrl = builder.Configuration["PaddleService:BaseUrl"];
  var baseUri = new Uri(baseUrl);
  client.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp/1.0");

  client.BaseAddress = baseUri;
});

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

public delegate IUpdateHandler HandlerResolver(Telegram.Bot.Types.Enums.UpdateType type);
