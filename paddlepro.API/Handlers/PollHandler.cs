using Telegram.Bot.Types;
using paddlepro.API.Services.Interfaces;

namespace paddlepro.API.Handlers;

public class PollHandler : IUpdateHandler
{
    ILogger<PollHandler> logger;
    ITelegramService telegramService;

    public PollHandler(
        ILogger<PollHandler> logger,
        ITelegramService telegramService
        )
    {
        this.logger = logger;
        this.telegramService = telegramService;
    }

    public async Task<bool> Handle(Update update)
    {
        return await this.telegramService.HandleReadyCheckVote(update);
    }
}
