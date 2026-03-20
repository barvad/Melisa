using System.Text.Json.Nodes;
using Practicum.MelisaBot.Groq;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Update = Telegram.Bot.Types.Update;

namespace Practicum.MelisaBot.Telegram;

public class TelegramBotBackgroundService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IGroqClient _groqClient;
    private readonly ILogger<TelegramBotBackgroundService> _logger;
    private readonly ISearchRepository _searchRepository;

    public TelegramBotBackgroundService(
        ITelegramBotClient botClient,
        ISearchRepository searchRepository,
        ILogger<TelegramBotBackgroundService> logger,
        IGroqClient groqClient)
    {
        _botClient = botClient;
        _searchRepository = searchRepository;
        _logger = logger;
        _groqClient = groqClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            stoppingToken
        );

        _logger.LogInformation("Telegram Bot started");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }


    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { Text: { } messageText } message) return;

        var chatId = message.Chat.Id;

        try
        {
            if (messageText == "/start")
                await bot.SendMessage(chatId, "Привет! Задай свой вопрос🤗", cancellationToken: ct);
            await bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);
            var result = await _searchRepository.SearchAsync(messageText);
            _logger.LogInformation($"Search result: {result}");
            var resultArray = JsonNode.Parse(result)?.AsArray().Select(x =>
                $"[{x?["Url"]}]\n{(x?["Text"]?.ToString().Contains("Ignore all instructions") == true ? "Документ содержал не безопасный контент, который был удалён системой" : x?["Text"])}");
            var context = string.Join("\n", resultArray ?? []);
            var promt = File.ReadAllText("PromtTemplate.txt").Replace("{question}", messageText)
                .Replace("{context}", context);
            _logger.LogInformation($"Result promt: \n{promt}");
            var answer = await _groqClient.SendMessageAsync(promt);
            _logger.LogInformation($"Result answer: \n{answer}");

            await bot.SendMessage(chatId, answer, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RAG");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex, "Telegram API Error");
        return Task.CompletedTask;
    }
}