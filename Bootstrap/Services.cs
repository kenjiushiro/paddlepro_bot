using paddlepro.API.Configurations;
using paddlepro.API.Handlers;
using paddlepro.API.Services;
using paddlepro.API.Services.Implementations;
using paddlepro.API.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace paddlepro.API.Bootstrap;

public static class ServicesConfiguration
{
  public static IServiceCollection AddServices(this IServiceCollection services, ConfigurationManager configuration)
  {
    services.AddScoped<IWeatherService, WeatherService>();
    services.AddScoped<IPaddleService, AtcService>();
    services.AddScoped<ITelegramService, TelegramService>();
    services.AddScoped<IAzureService, AzureService>();

    services.AddMemoryCache();

    services.AddScoped<IUpdateContextService, UpdateContextService>();

    services.AddSingleton<ITelegramBotClient>(sp =>
    {
      // TODO this might have a cleaner way to inject, read docu
      var botToken = configuration["BotConfiguration:BotToken"];
      return new TelegramBotClient(botToken!);
    });

    services.AddHttpClient("WeatherServiceHealth", client =>
    {
      var baseUrl = configuration["WeatherService:BaseUrl"];
      client.BaseAddress = new Uri(baseUrl!);
      client.Timeout = TimeSpan.FromSeconds(5); // Shorter timeout for health checks
    });

    services.AddHttpClient("PaddleServiceHealth", client =>
    {
      var baseUrl = configuration["PaddleService:BaseUrl"];
      client.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp/1.0");
      client.BaseAddress = new Uri(baseUrl!);
    });

    services.AddHttpClient<IPaddleService, AtcService>(client =>
    {
      var baseUrl = configuration["PaddleService:BaseUrl"];
      client.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp/1.0");
      client.BaseAddress = new Uri(baseUrl!);
    });

    services.AddHttpClient<IWeatherService, WeatherService>(client =>
    {
      var baseUrl = configuration["WeatherService:BaseUrl"];
      client.BaseAddress = new Uri(baseUrl!);
    });

    return services;
  }

  public static IServiceCollection AddConfiguration(this IServiceCollection services, ConfigurationManager configuration)
  {
    services.Configure<WeatherServiceConfiguration>(configuration.GetSection("WeatherService"));
    services.Configure<TelegramConfiguration>(configuration.GetSection("Telegram"));
    services.Configure<AtcServiceConfiguration>(configuration.GetSection("PaddleService"));
    services.Configure<AzureConfiguration>(configuration.GetSection("Azure"));

    return services;
  }


  public static IServiceCollection AddHandlers(this IServiceCollection services)
  {
    services.AddScoped<MessageHandler>();
    services.AddScoped<CallbackQueryHandler>();
    services.AddScoped<PollHandler>();

    services.AddScoped<UpdateDispatcher>();
    services.AddTransient<HandlerResolver>(serviceProvider => key =>
    {
      switch (key)
      {
        case UpdateType.Message:
          return serviceProvider.GetService<MessageHandler>()!;
        case UpdateType.CallbackQuery:
          return serviceProvider.GetService<CallbackQueryHandler>()!;
        case UpdateType.Poll:
          return serviceProvider.GetService<PollHandler>()!;
        default:
          return null;
      }
    });

    return services;
  }
}
