namespace paddlepro.API.Models;

public enum BotMessageType
{
  DayPicker,
  ReadyCheckPoll,
  CountMessage,
  ClubMessage,
  CourtMessage,
  HourPicker,
}

public class BotMessage
{
  public int Id { get; set; }
  public BotMessageType Type { get; set; }
}

public class UpdateContext
{
  public UpdateContext()
  {
    this.Messages = new BotMessage[] { };
  }

  public void ClearMessages()
  {
    this.Messages = Array.Empty<BotMessage>();
  }

  public int[] GetMessages(BotMessageType type)
  {
    return this.Messages.Where(m => m.Type == type).Select(x => x.Id).ToArray();
  }

  public void AddMessage(int MessageId, BotMessageType type)
  {
    this.Messages = this.Messages.Append(new BotMessage
    {
      Id = MessageId,
      Type = type,
    }).ToArray();
  }

  public long? ChatId { get; set; }
  public int? MessageThreadId { get; set; }
  public string SelectedDate { get; set; } = "";
  public string NextStep { get; set; } = "";
  public BotMessage[] Messages { get; set; }
}

