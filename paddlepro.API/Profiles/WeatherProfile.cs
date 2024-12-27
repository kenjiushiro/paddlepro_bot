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
      { "Patchy rain nearby", "☔" },
      { "Moderate rain", "🌧️" },
      { "Stormy", "⛈️" },
      { "Cloudy", "⛈️"},
      { "Partly Cloudy", "⛅" },
    };

        CreateMap<ForecastDay, WeatherForecast>()
          .ForMember(wf => wf.Day, opt => opt.MapFrom(f => f.Date))
          .ForMember(wf => wf.MaxTemp, opt => opt.MapFrom(f => (short)Math.Round(f.Day.MaxTempC)))
          .ForMember(wf => wf.MinTemp, opt => opt.MapFrom(f => (short)Math.Round(f.Day.MinTempC)))
          .ForMember(wf => wf.ChanceOfRain, opt => opt.MapFrom(f => (short)f.Day.DailyChanceOfRain))
          .ForMember(wf => wf.Emoji, opt => opt.MapFrom(f => emojiDict[f.Day.Condition.Text.Trim()]));
    }
}
