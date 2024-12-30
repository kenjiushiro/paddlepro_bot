namespace paddlepro.API.Models.Application;

public class Availability
{
  public DateTime Date { get; set; }
  public Club[] Clubs { get; set; }
}

public class Club
{
  public string Id { get; set; }
  public string Name { get; set; } = "";
  public Location Location { get; set; }
  public string PhoneNumber { get; set; } = "";
  public string Notes { get; set; } = "";
  public Court[] Courts { get; set; } = new Court[0];
}

public class CourtAvailability
{
  public short Duration { get; set; }
  public string Start { get; set; } = "";
  public float Price { get; set; }
}

public class Court
{
  public string Id { get; set; } = "";
  public bool Haslighting { get; set; }
  public bool IsRoofed { get; set; }
  public string Name { get; set; } = "";
  public string SurfaceType { get; set; } = "";
  public bool IsBeelup { get; set; }

  public CourtAvailability[] Availability { get; set; }
}

public class Location
{
  public string Address { get; set; } = "";
  public float Lat { get; set; }
  public float Long { get; set; }
}

