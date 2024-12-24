namespace paddlepro.API.Models.Infrastructure;

public class Condition
{
  public string Text { get; set; } = "";
  public string Icon { get; set; } = "";
}

public class DayWeather
{
  public float maxtemp_c { get; set; }
  public double mintemp_c { get; set; }
  public int daily_chance_of_rain { get; set; }
  public int DailyWillItRain { get; set; }
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
