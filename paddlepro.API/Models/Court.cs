namespace paddlepro.API.Models;

public class Court
{
    public string Name { get; set; } = "";
    public string Notes { get; set; } = "";
    public Availability[] Availability { get; set; }
}
