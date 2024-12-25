namespace paddlepro.API.Configurations;

public class WeatherServiceConfiguration
{
  public string BaseUrl { get; set; } = "";
  public string ApiKey { get; set; } = "";
  public short DaysInAdvance { get; set; }
}
