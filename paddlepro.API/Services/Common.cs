using paddlepro.API.Helpers;

namespace paddlepro.API.Services;

public static class Common
{
  public static Dictionary<string, long?> pollChatIdDict = new Dictionary<string, long?>();
  public const string PICK_COURT_COMMAND = "CourtPick";
  public static readonly string PICK_HOUR_COMMAND = "HourPick";
  public static readonly string PICK_DATE_COMMAND = "DatePick";
  public static readonly string SCHEDULE_REMINDER_COMMAND = "ScheduleReminder";

  private static readonly string CALLBACK_DELIMITER = ";";

  public static string EncodeCallback(string action, string data)
  {
    return $"{action}{CALLBACK_DELIMITER}{data}";
  }

  public static (string, string) DecodeCallback(string callback)
  {
    return callback.SplitBy(CALLBACK_DELIMITER);
  }
}
