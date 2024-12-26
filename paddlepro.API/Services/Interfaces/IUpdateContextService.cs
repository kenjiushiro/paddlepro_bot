using paddlepro.API.Models;
using Telegram.Bot.Types;

namespace paddlepro.API.Services.Interfaces;

public interface IUpdateContextService
{
    void SetChatContext(Update update);
    UpdateContext GetChatContext(Update update);
}
