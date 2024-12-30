namespace paddlepro.API.Models.Application;

public class WeatherForecast
{
  public string Day { get; set; } = "";
  public short MinTemp { get; set; }
  public short MaxTemp { get; set; }
  public string Emoji { get; set; } = "";
  public short ChanceOfRain { get; set; }
}
