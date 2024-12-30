using System.Text.Json.Serialization;

namespace paddlepro.API.Models.Infrastructure;

public class Condition
{
  public string Text { get; set; } = "";
  public string Icon { get; set; } = "";
}

public class DayWeather
{
  [JsonPropertyName("maxtemp_c")]
  public float MaxTempC { get; set; }

  [JsonPropertyName("mintemp_c")]
  public double MinTempC { get; set; }

  [JsonPropertyName("daily_chance_of_rain")]
  public int DailyChanceOfRain { get; set; }

  public Condition Condition { get; set; }
}
public class ForecastDay
{
  public string Date { get; set; } = "";
  public DayWeather Day { get; set; }
}

public class Forecast
{
  public ForecastDay[] Forecastday { get; set; }
}

public class WeatherApiResponse
{
  public Forecast Forecast { get; set; }
}
