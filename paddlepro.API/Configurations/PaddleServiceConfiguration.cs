namespace paddlepro.API.Configurations;

public class PaddleServiceConfiguration
{
  public string BaseUrl { get; set; } = "";
  public string PlaceId { get; set; } = "";
  public string Path { get; set; } = "";
  public int DaysInAdvance { get; set; }
  public int PlayerCount { get; set; }
  public int[] ClubIds { get; set; } = new int[0];
}
