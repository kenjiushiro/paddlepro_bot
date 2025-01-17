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
    var commands = new Dictionary<string, Func<Update, string, Task<bool>>>
        {
          { Common.PICK_DATE_COMMAND, this.telegramService.HandleDatePick },
          { Common.PICK_CLUB_COMMAND, this.telegramService.HandleClubPick },
          { Common.PICK_COURT_COMMAND, this.telegramService.HandleCourtPick },
          { Common.PICK_HOUR_COMMAND, this.telegramService.HandleHourPick },
          { Common.PIN_REMINDER_COMMAND, this.telegramService.HandlerPinReminderPick },
        };
    (var action, string callbackData) = (update?.CallbackQuery?.Data!).DecodeCallback();

    if (!commands.TryGetValue(action, out var actionHandler))
    {
      this.logger.LogWarning("Couldn't find callback query action {Action}", action);
      return false;
    }

    this.logger.LogInformation("Callback received: {Action}", action);
    return await actionHandler(update!, callbackData);
  }
}
