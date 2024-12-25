namespace paddlepro.API.Configurations;

public class AtcServiceConfiguration
{
  public string BaseUrl { get; set; } = "";
  public string PlaceId { get; set; } = "";
  public string ListPath { get; set; } = "";
  public string CheckoutPath { get; set; } = "";
  public int DaysInAdvance { get; set; }
  public int PlayerCount { get; set; }
  public string SportId { get; set; }
  public string[] ClubIds { get; set; } = new string[0];
}
