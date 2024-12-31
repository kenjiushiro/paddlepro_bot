using Telegram.Bot.Types;
using paddlepro.API.Services;
using paddlepro.API.Helpers;
using paddlepro.API.Services.Interfaces;

namespace paddlepro.API.Handlers;

public class CallbackQueryHandler : IUpdateHandler
{
    private readonly ILogger<CallbackQueryHandler> logger;
    private readonly ITelegramService telegramService;

    public CallbackQueryHandler(
        ILogger<CallbackQueryHandler> logger,
        ITelegramService telegramService
        )
    {
        this.logger = logger;
        this.telegramService = telegramService;
    }

    public async Task<bool> Handle(Update update)
    {
        Dictionary<string, Func<Update, Task<bool>>> commands = new Dictionary<string, Func<Update, Task<bool>>>
        {
          { Common.PICK_DATE_COMMAND, this.telegramService.HandleDatePick },
          { Common.PICK_COURT_COMMAND, this.telegramService.HandleCourtPick },
          { Common.PICK_HOUR_COMMAND, this.telegramService.HandleHourPick },
          { Common.PIN_REMINDER_COMMAND, this.telegramService.SendPinnedMatchReminderMessage },
        };
        (var action, var _) = (update?.CallbackQuery?.Data!).DecodeCallback();

        if (!commands.TryGetValue(action, out var actionHandler))
        {
            this.logger.LogWarning("Couldn't find callback query action {Action}", action);
            return false;
        }

        this.logger.LogInformation("Callback received: {Action}", action);
        return await actionHandler(update!);
    }
}
