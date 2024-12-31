using paddlepro.API.Models.Application;
using Telegram.Bot.Types.ReplyMarkups;

namespace paddlepro.API.Services.Interfaces;

public interface IMessageFormatterService
{
    (string, InlineKeyboardMarkup) FormatDayPicker(WeatherForecast[] forecast, int daysCount);
    (string, InlineKeyboardMarkup) FormatClubsAvailabilityMessage(Club[] clubs, string date);
    (string, InlineKeyboardMarkup) FormatCourtsAvailabilityMessage(Club club, string selectedDate);
    (string, InlineKeyboardMarkup) FormatMatchPickerMessage(Club club, Court court, string selectedDate);
    string FormatPinnedMessageReminder(Club club, Court court, string selectedDate, string start, string duration);
    (string, InlineKeyboardMarkup) FormatSendReservationActions(Club club, Court court, string selectedDate, string start, string duration, string url);
}
