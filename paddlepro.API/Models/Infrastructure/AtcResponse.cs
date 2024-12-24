namespace paddlepro.API.Models.Infrastructure;

public class Location
{
    public string Name { get; set; } = "";
    public float Lat { get; set; }
    public float Lng { get; set; }
}

public class PageProps
{
    public string PlaceId { get; set; } = "";
    public string LocationName { get; set; } = "";
}

public class BusinessHours
{
    public string DayOfWeek { get; set; } = "";
    public string OpenTime { get; set; } = "";
    public string CloseTime { get; set; } = "";
}

public class Court
{
    public string Id { get; set; } = "";
    public bool Haslighting { get; set; }
    public bool IsRoofed { get; set; }
    public string Name { get; set; } = "";
    public string SurfaceType { get; set; } = "";
    public bool IsBeelup { get; set; }
}

public class Price
{
    public int Cents { get; set; }
    public string Currency { get; set; } = "";

}

public class AvailableSlots
{
    public short Duration { get; set; }
    public string Start { get; set; } = "";
    public Price Price { get; set; }
    public string CourtId { get; set; } = "";
}

public class BookingBySport
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string State { get; set; } = "";
    public string Phone { get; set; } = "";
    public Location Location { get; set; }
    public string Permalink { get; set; } = "";
    public bool HasBeelup { get; set; }
    public BusinessHours BusinessHoursForGivenDate { get; set; }
    public Court[] Courts { get; set; }
}

public class AtcResponse
{
    public PageProps PageProps { get; set; }
}
