namespace paddlepro.API.Configurations;

public class Commands
{
  public string Search { get; set; } = "";
  public string ReadyCheck { get; set; } = "";

}
public class TelegramConfiguration
{
  public Commands Commands { get; set; }
  public bool DeleteMessages { get; set; }
}
