using paddlepro.API.Models.Application;
using paddlepro.API.Helpers;
using paddlepro.API.Services.Interfaces;
using Telegram.Bot.Types.ReplyMarkups;

namespace paddlepro.API.Services.Implementations;

public class MessageFormatterService : IMessageFormatterService
{
    public MessageFormatterService()
    {
    }

    public (string, InlineKeyboardMarkup) FormatClubsAvailabilityMessage(Club[] clubs, string date)
    {

        var buttons = clubs.Select(c =>
        {
            return new[]
              {
                InlineKeyboardButton.WithCallbackData($"{c.Name}", (Common.PICK_CLUB_COMMAND, c.Id).EncodeCallback()),
                };
        });
        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons);

        var message = clubs.Select(club => GetClubMessage(club)).Where(m => !string.IsNullOrEmpty(m)).Join("\n\n");
        return (message.MdEscapeChars(), inlineKeyboard);
    }

    public (string, InlineKeyboardMarkup) FormatMatchPickerMessage(Club club, Court court, string selectedDate)
    {
        var buttons = court?.Availability.GroupBy(a => a.Start).Select(
            g => g.Select(a =>
              InlineKeyboardButton.WithCallbackData($"{a.Start} {a.Duration}'", (Common.PICK_HOUR_COMMAND, $"{club.Id}+{court.Id}+{a.Start}+{a.Duration}").EncodeCallback())
                          )
            );

        var message = @$"📅{selectedDate.MdBold()}
🏟️{club?.Name}
🎾_{court?.Name}_ ";
        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons!);

        return (message.MdEscapeChars(), inlineKeyboard);
    }

    public (string, InlineKeyboardMarkup) FormatCourtsAvailabilityMessage(Club club, string selectedDate)
    {
        var courts = club?.Courts.Where(c => c.IsAvailable);
        var buttons = courts?.Select(
            court =>
            {
                var value = $"{club?.Id}:{court.Id}";
                return new[]
                    {
                    InlineKeyboardButton.WithCallbackData($"{court.Name}", (Common.PICK_COURT_COMMAND, value).EncodeCallback()),
              };
            }).ToArray();

        var message = @$"📅 {selectedDate}
🏟️{club?.Name}
{courts?.Select(c => GetCourtMessage(c)).Join("\n")}";

        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttons!);

        return (message.MdEscapeChars(), inlineKeyboard);
    }

    public (string, InlineKeyboardMarkup) FormatDayPicker(WeatherForecast[] forecast, int daysCount)
    {
        var inlineKeyboard = new InlineKeyboardMarkup();

        DateTime startDate = DateTime.UtcNow;
        for (var i = 0; i < daysCount; i++)
        {
            var date = startDate.AddDays(i);
            var buttonDisplay = date.ToString("dddd dd-MM", new System.Globalization.CultureInfo("es-ES"));
            var buttonValue = date.ToString("yyyy-MM-dd");
            var dayForecast = forecast.SingleOrDefault(f => f.Day == buttonValue);

            var rainEmoji = "";

            buttonDisplay = $"{buttonDisplay} {dayForecast?.Emoji} {dayForecast?.MinTemp}°C-{dayForecast?.MaxTemp}°C {rainEmoji} {dayForecast?.ChanceOfRain}%";
            inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData(buttonDisplay, (Common.PICK_DATE_COMMAND, buttonValue).EncodeCallback()));
        }

        return ("Elegi dia", inlineKeyboard);
    }

    private string GetClubMessage(Club club)
    {
        var hoursAvailable = club
          .Courts
          .SelectMany(c => c.Availability)
          .GroupBy(a => a.Start)
          .Select(a => $"{a.Key} {a.Select(b => b.Duration.ToString() + "min").Distinct().Join(" - ")}")
          .Join("\n>");
        return @$"
*🏟️ {club.Name}
📍 {club.Location.Address}*
{hoursAvailable.MdExpandable()}";
    }


    private string GetCourtMessage(Models.Application.Court court)
    {
        var roofed = court.IsRoofed ? "Techada" : "No techada";

        return @$"🎾{court.Name.MdBold()} {roofed.MdItalic()}
{GetAvailabilityMessage(court).MdExpandable()}
";
    }

    private string GetAvailabilityMessage(Models.Application.Court court)
    {
        return court.Availability.GroupBy(a => a.Start).Select(a => @$"
{a.Key.MdBold()} - {a.Select(b => b.Duration + "min").Join(" - ")}").Join("");
    }

    public string FormatPinnedMessageReminder(Club club, Court court, string selectedDate, string start, string duration)
    {

        return @$"Detalles del partido:
📅 *{selectedDate}*
📍 {club?.Location.Address}
🏟️ {club?.Name}
🎾 {court?.Name}
🕒{start}
⏱️{duration}min
        ";

    }

    public (string, InlineKeyboardMarkup) FormatSendReservationActions(Club club, Court court, string selectedDate, string start, string duration, string url)
    {
        string callbackValue = $"{club.Id}+{court.Id}+{start}+{duration}";
        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup();
        inlineKeyboard.AddNewRow(InlineKeyboardButton.WithUrl("📅Reservar", url));
        inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("📍Pinear mensaje", (Common.PIN_REMINDER_COMMAND, callbackValue).EncodeCallback()));
        var message = @$"Reservar
📅{selectedDate} 🕒{start} ⏱️{duration}min
🏟️{club.Name}
🎾{court?.Name}
";
        return (message, inlineKeyboard);
    }
}
