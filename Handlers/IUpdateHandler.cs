using Telegram.Bot.Types;

namespace paddlepro.API.Handlers;

public interface IUpdateHandler
{
  Task<bool> Handle(Update update);
}
