namespace paddlepro.API.Services;

public static class Common
{
  public static Dictionary<string, long?> pollChatIdDict = new Dictionary<string, long?>();
  public const string PICK_CLUB_COMMAND = "ClubPick";
  public const string PICK_COURT_COMMAND = "CourtPick";
  public static readonly string PICK_HOUR_COMMAND = "HourPick";
  public static readonly string PICK_DATE_COMMAND = "DatePick";
  public static readonly string PIN_REMINDER_COMMAND = "PinReminder";
}
