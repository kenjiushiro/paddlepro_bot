using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using paddlepro.API.Configurations;
using paddlepro.API.Models;
using paddlepro.API.Services.Interfaces;
using paddlepro.API.Models.Application;
using AutoMapper;

namespace paddlepro.API.Services.Implementations;

public class TelegramService : ITelegramService
{
    ITelegramBotClient _botClient;
    ILogger<TelegramService> _logger;
    PaddleServiceConfiguration _paddleConfig;
    TelegramConfiguration _telegramConfig;
    IPaddleService _paddleService;
    IContextService _contextService;
    IWeatherService _weatherService;
    IMapper _mapper;

    private static Dictionary<string, long?> pollChatIdDict = new Dictionary<string, long?>();
    List<InputPollOption> options = new List<InputPollOption> { new InputPollOption("Si"), new InputPollOption("No") };

    public TelegramService(
        ITelegramBotClient botClient,
        IPaddleService paddleService,
        IWeatherService weatherService,
        IContextService contextService,
        ILogger<TelegramService> logger,
        IOptions<PaddleServiceConfiguration> paddleConfig,
        IOptions<TelegramConfiguration> telegramConfig,
        IMapper mapper
        )
    {
        _botClient = botClient;
        _logger = logger;
        _contextService = contextService;
        _paddleService = paddleService;
        _weatherService = weatherService;
        _paddleConfig = paddleConfig.Value;
        _telegramConfig = telegramConfig.Value;
        _mapper = mapper;
    }

    // TODO better organize all these methods
    public async Task<bool> Respond(Update update)
    {
        if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
        {
            var chatId = update?.Message?.Chat.Id;
            var threadId = update?.Message?.MessageThreadId;
            var command = update?.Message?.Text ?? "";

            if (!command.Contains(_telegramConfig.Commands.ReadyCheck) && !command.Contains(_telegramConfig.Commands.Search))
            {
                return false;
            }
            _logger.LogInformation("Command: {Command}", command);
            _contextService.SetChatContext(chatId, threadId, "", command);
            await SendAvailableDates(chatId);
        }
        else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Poll)
        {
            await HandlePollAnswer(update);
        }
        else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.PollAnswer)
        {
        }
        else
        {
            // Date selected
            var chatId = update?.CallbackQuery?.Message?.Chat.Id;
            await OnDateSelected(update);
            var context = _contextService.GetChatContext(chatId);
            if (context.LastCommand.Contains(_telegramConfig.Commands.ReadyCheck))
            {
                await StartReadyCheckPoll(chatId);
            }
            else if (context.LastCommand.Contains(_telegramConfig.Commands.Search))
            {
                await Search(context);
            }
        }
        return true;
    }

    private string EscapeCharsForMarkdown(string body)
    {
        return body.Replace("-", "\\-").Replace(".", "\\.").Replace("(", "\\(").Replace(")", "\\)");

    }
    private string GetAvailabilityMessage(Models.Application.Court court)
    {
        return string.Join("\n", court.Availability.Select(a => @$">{a.Start} - {a.Duration}min - ${a.Price}"));
    }

    private string GetCourtMessage(Models.Application.Court court)
    {
        var roofed = court.IsRoofed ? "Techada" : "No techada";

        return @$">{court.Name} - {roofed}
{GetAvailabilityMessage(court)}";
    }

    private string GetMessage(Club[] clubs, string date)
    {
        return string.Join("\n", clubs
            .Select(club => @$"
*🏟️ {club.Name} - 📍 {club.Location.Address} - {date}*
**>Canchas:
{string.Join("\n", club.Courts.Select(c => GetCourtMessage(c)))}||"));
    }

    public async Task Search(Context context)
    {
        var availability = _mapper.Map<Availability>(await _paddleService.GetAvailability(context.SelectedDate));
        var clubs = availability.Clubs.Where(x => _paddleConfig.ClubIds.ToList().Contains(x.Id)).ToArray();
        var message = EscapeCharsForMarkdown(GetMessage(clubs, context.SelectedDate));
        _logger.LogInformation(message);

        var response = await _botClient.SendMessage(context.ChatId, message, messageThreadId: context.MessageThreadId, disableNotification: true, parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2);
    }

    public async Task SetDate(long? chatId)
    {
        var context = _contextService.GetChatContext(chatId);
        var message = await _botClient.SendMessage(chatId, $"Se juega x dia", messageThreadId: context.MessageThreadId);
        await _botClient.PinChatMessage(chatId, message.MessageId);
    }

    public async Task OnDateSelected(Update update)
    {
        var chatId = update?.CallbackQuery?.Message?.Chat.Id;
        var threadId = update?.CallbackQuery?.Message?.MessageThreadId;

        var matchDate = update?.CallbackQuery?.Data;

        if (chatId == null)
        {
            _logger.LogWarning("Chat ID null for update {Id} on ReadyCheckPoll step", update.Id);
        }

        if (matchDate == null)
        {
            _logger.LogWarning("Match Date returned null from Update Id {Id}", update.Id);
        }

        var context = _contextService.GetChatContext(chatId);
        context.SelectedDate = matchDate;
        if (context.LatestDayPicker > 0)
        {
            _botClient.DeleteMessage(context.ChatId, context.LatestDayPicker);
            context.LatestDayPicker = 0;
        }
    }

    public async Task StartReadyCheckPoll(long? chatId)
    {
        var context = _contextService.GetChatContext(chatId);
        if (context.LatestPollId > 0)
        {
            _botClient.DeleteMessage(context.ChatId, context.LatestPollId);
            context.LatestPollId = 0;
        }

        var poll = await _botClient.SendPoll(context.ChatId, $"Estas para jugar el {context.SelectedDate}?", options, isAnonymous: false, messageThreadId: context.MessageThreadId, disableNotification: true);
        context.LatestPollId = poll.MessageId;
        pollChatIdDict.Add(poll.Poll.Id, chatId);
        await SendPlayerCountMessage(context);
    }

    public async Task SendAvailableDates(long? chatId)
    {
        var dateRange = DateTime.Today.AddDays(_paddleConfig.DaysInAdvance);
        var forecast = _mapper.Map<WeatherForecast[]>(await _weatherService.GetWeatherForecast());

        var context = _contextService.GetChatContext(chatId);

        var inlineKeyboard = new InlineKeyboardMarkup();

        DateTime startDate = DateTime.UtcNow;
        for (var i = 0; i < _paddleConfig.DaysInAdvance; i++)
        {
            var date = startDate.AddDays(i);
            var buttonDisplay = date.ToString("dddd dd-MM", new System.Globalization.CultureInfo("es-ES"));
            var buttonValue = date.ToString("yyyy-MM-dd");
            var dayForecast = forecast.SingleOrDefault(f => f.Day == buttonValue);

            var rainEmoji = "";

            buttonDisplay = $"{buttonDisplay} {dayForecast?.Emoji} {dayForecast?.MinTemp}°C-{dayForecast?.MaxTemp}°C {rainEmoji} {dayForecast?.ChanceOfRain}%";
            inlineKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData(buttonDisplay, buttonValue));
        }

        var message = await _botClient.SendMessage(chatId, "Elegi dia", messageThreadId: context.MessageThreadId, replyMarkup: inlineKeyboard, disableNotification: true);
        context.LatestDayPicker = message.MessageId;
    }

    private async Task SendPlayerCountMessage(Context context, string messageText = "Faltan 4 votos")
    {
        var countMessage = await _botClient.SendMessage(context.ChatId, messageText, messageThreadId: context.MessageThreadId, disableNotification: true);
        context.CountMessageId = countMessage.MessageId;
    }

    public async Task HandlePollAnswer(Update update)
    {
        var count = update?.Poll?.Options.Single(o => o.Text == "Si").VoterCount;
        if (!pollChatIdDict.TryGetValue(update.Poll.Id, out var chatId))
        {
            _logger.LogWarning("Poll ID {Id} not found", update.Poll.Id);
            return;
        }
        var context = _contextService.GetChatContext(chatId);

        if (count < _paddleConfig.PlayerCount)
        {
            var messageText = $"Faltan {_paddleConfig.PlayerCount - count} votos";
            await _botClient.EditMessageText(context.ChatId, context.CountMessageId, messageText);
        }
        else
        {
            await Search(context);
        }
    }
}
