namespace paddlepro.API.Configurations;

public class TextAnalytics
{
  public string Endpoint { get; set; } = "";
  public string Key { get; set; } = "";

}
public class AzureConfiguration
{
  public TextAnalytics TextAnalytics { get; set; }
}
