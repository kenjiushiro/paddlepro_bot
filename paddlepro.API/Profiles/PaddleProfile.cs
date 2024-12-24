using paddlepro.API.Models.Infrastructure;
using paddlepro.API.Models.Application;
using AutoMapper;

namespace paddlepro.API.Profiles;

public class PaddleProfile : Profile
{
  public PaddleProfile()
  {
    CreateMap<AtcResponse, Availability>()
      .ForMember(x => x.Clubs, opt => opt.MapFrom(x => x.PageProps.BookingsBySport));

    CreateMap<AtcBookingBySport, Club>()
      .ForMember(x => x.Id, opt => opt.MapFrom(x => x.Id))
      .ForMember(x => x.Name, opt => opt.MapFrom(x => x.Name))
      .ForMember(x => x.PhoneNumber, opt => opt.MapFrom(x => x.Phone))
      .ForMember(x => x.Courts, opt => opt.MapFrom(x => x.Courts.Values));

    CreateMap<AtcLocation, Location>()
      .ForMember(x => x.Address, opt => opt.MapFrom(x => x.Name))
      .ForMember(x => x.Lat, opt => opt.MapFrom(x => x.Lat))
      .ForMember(x => x.Long, opt => opt.MapFrom(x => x.Lng));

    CreateMap<AtcAvailableSlots, CourtAvailability>();

    CreateMap<AtcCourt, Court>()
      .ForMember(x => x.Availability, opt => opt.Ignore());
  }
}
