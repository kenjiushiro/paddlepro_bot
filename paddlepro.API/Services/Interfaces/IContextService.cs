using paddlepro.API.Models;

namespace paddlepro.API.Services.Interfaces;

public interface IContextService
{
  void SetChatContext(long? chatId, int? messageThreadId, string selectedDate, string lastCommand);
  Context GetChatContext(long? chatId);
}
