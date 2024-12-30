using paddlepro.API.Models;
using Telegram.Bot.Types;

namespace paddlepro.API.Services.Interfaces;

public interface IUpdateContextService
{
  UpdateContext GetChatContext(Update update);
}
