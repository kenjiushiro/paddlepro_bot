namespace paddlepro.API.Models;

public class Context
{
    public long? ChatId { get; set; }
    public int? MessageThreadId { get; set; }
    public string SelectedDate { get; set; } = "";
    public string LastCommand { get; set; } = "";
    public int LatestPollId { get; set; }
    public int LatestDayPicker { get; set; }
}

