namespace paddlepro.API.Services;

public class Context
{
  public long? ChatId { get; set; }
  public int? MessageThreadId { get; set; }
  public string SelectedDate { get; set; } = "";
  public string LastCommand { get; set; } = "";
}

public interface IContextService
{
  void SetChatContext(long? chatId, int? messageThreadId, string selectedDate, string lastCommand);
  Context GetChatContext(long? chatId);
}
