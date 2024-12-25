using paddlepro.API.Models.Infrastructure;
using paddlepro.API.Models.Application;
using AutoMapper;

namespace paddlepro.API.Profiles;

public class PaddleProfile : Profile
{
    public PaddleProfile()
    {
        CreateMap<AtcResponse, Availability>()
          .ForMember(x => x.Clubs, opt => opt.MapFrom(x => x.PageProps.BookingsBySport))
          .AfterMap((source, destiny, context) =>
          {
              foreach (var club in destiny.Clubs)
              {
                  var sourceClub = source.PageProps.BookingsBySport.First(c => c.Id == club.Id);
                  foreach (var court in club.Courts)
                  {
                      var availableSlots = sourceClub.AvailableSlots.Where(c => c.CourtId == court.Id).ToArray();
                      court.Availability = context.Mapper.Map<CourtAvailability[]>(availableSlots);
                  }
              }
          });

        CreateMap<AtcBookingBySport, Club>()
          .ForMember(x => x.Id, opt => opt.MapFrom(x => x.Id))
          .ForMember(x => x.Name, opt => opt.MapFrom(x => x.Name))
          .ForMember(x => x.PhoneNumber, opt => opt.MapFrom(x => x.Phone))
          .ForMember(x => x.Courts, opt => opt.MapFrom(x => x.Courts.Values));

        CreateMap<AtcLocation, Location>()
          .ForMember(x => x.Address, opt => opt.MapFrom(x => x.Name))
          .ForMember(x => x.Lat, opt => opt.MapFrom(x => x.Lat))
          .ForMember(x => x.Long, opt => opt.MapFrom(x => x.Lng));

        CreateMap<AtcAvailableSlots, CourtAvailability>()
          .ForMember(x => x.Price, opt => opt.MapFrom(x => x.Price.Cents / 100))
          .ForMember(x => x.Start, opt => opt.MapFrom(x => DateTime.Parse(x.Start).ToString("HH:mm")));

        CreateMap<AtcCourt, Court>()
          .ForMember(x => x.Availability, opt => opt.Ignore());
    }
}
