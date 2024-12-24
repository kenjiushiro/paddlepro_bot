using paddlepro.API.Models.Infrastructure;
using paddlepro.API.Models.Application;
using AutoMapper;

namespace paddlepro.API.Profiles;

public class WeatherProfile : Profile
{
  public WeatherProfile()
  {
    Dictionary<string, string> emojiDict = new Dictionary<string, string>{
      { "Sunny", "☀️"},
      { "Patchy rain nearby", "🌧️" },
    };

    CreateMap<ForecastDay, WeatherForecast>()
      .ForMember(wf => wf.Day, opt => opt.MapFrom(f => f.Date))
      .ForMember(wf => wf.MaxTemp, opt => opt.MapFrom(f => (short)Math.Round(f.Day.maxtemp_c)))
      .ForMember(wf => wf.MinTemp, opt => opt.MapFrom(f => (short)Math.Round(f.Day.mintemp_c)))
      .ForMember(wf => wf.ChanceOfRain, opt => opt.MapFrom(f => (short)f.Day.daily_chance_of_rain))
      .ForMember(wf => wf.Emoji, opt => opt.MapFrom(f => emojiDict[f.Day.Condition.Text]));
  }
}
