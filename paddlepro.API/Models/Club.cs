namespace paddlepro.API.Models;

public class Club
{
  public int Id { get; set; }
  public string Name { get; set; } = "";
  public string Address { get; set; } = "";
  public string Notes { get; set; } = "";
  public Court[] Courts { get; set; } = new Court[0];

}
