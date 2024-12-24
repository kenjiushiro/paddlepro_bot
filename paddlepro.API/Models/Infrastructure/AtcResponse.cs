using System.Text.Json.Serialization;

namespace paddlepro.API.Models.Infrastructure;

public class AtcLocation
{
  public string Name { get; set; } = "";
  public float Lat { get; set; }
  public float Lng { get; set; }
}

public class AtcPageProps
{
  public string PlaceId { get; set; } = "";
  public string LocationName { get; set; } = "";
  public AtcBookingBySport[] BookingsBySport { get; set; }
}

public class AtcBusinessHours
{
  public string DayOfWeek { get; set; } = "";
  public string OpenTime { get; set; } = "";
  public string CloseTime { get; set; } = "";
}

public class AtcPrice
{
  public int Cents { get; set; }
  public string Currency { get; set; } = "";
}

public class AtcAvailableSlots
{
  public short Duration { get; set; }
  public string Start { get; set; } = "";
  public AtcPrice Price { get; set; }
  public string CourtId { get; set; } = "";
}

public class AtcBookingBySport
{
  public string Id { get; set; } = "";
  public string Name { get; set; } = "";
  public string State { get; set; } = "";
  public string Phone { get; set; } = "";
  public AtcLocation Location { get; set; }
  public string Permalink { get; set; } = "";

  public AtcBusinessHours BusinessHoursForGivenDate { get; set; }

  public Dictionary<string, AtcCourt> Courts { get; set; }

  public AtcAvailableSlots AvailableSlots { get; set; }
}

public class AtcCourt
{
  public string Id { get; set; } = "";

  [JsonPropertyName("Haslighting")]
  public bool Haslighting { get; set; }

  [JsonPropertyName("is_roofed")]
  public bool IsRoofed { get; set; }

  public string Name { get; set; } = "";

  [JsonPropertyName("surface_type")]
  public string SurfaceType { get; set; } = "";

  [JsonPropertyName("is_beelup")]
  public bool IsBeelup { get; set; }
}

public class AtcResponse
{
  public AtcPageProps PageProps { get; set; }
}
