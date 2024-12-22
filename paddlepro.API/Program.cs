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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

/*app.UseHttpsRedirection();*/

app.UseAuthorization();

app.MapControllers();

app.Run();
