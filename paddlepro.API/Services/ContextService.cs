using paddlepro.API.Models;

namespace paddlepro.API.Services;

public class ContextService : IContextService
{
    private readonly List<Context> contexts;
    public ContextService()
    {
        this.contexts = new List<Context>();
    }

    public void SetChatContext(long? chatId, int? messageThreadId, string selectedDate, string lastCommand)
    {
        var context = GetChatContext(chatId);
        if (context != null)
        {
            context.MessageThreadId = messageThreadId;
            context.SelectedDate = selectedDate;
            context.LastCommand = lastCommand;
        }
        else
        {
            this.contexts.Add(new Context
            {
                ChatId = chatId,
                MessageThreadId = messageThreadId,
                SelectedDate = selectedDate,
                LastCommand = lastCommand,
            });

        }
    }

    public Context GetChatContext(long? chatId)
    {
        return this.contexts.FirstOrDefault(c => c.ChatId == chatId);
    }
}
