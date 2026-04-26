using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");

if (string.IsNullOrEmpty(botToken))
{
    Console.WriteLine("Token not found");
    return;
}

var botClient = new TelegramBotClient(botToken);

using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token
);

Console.WriteLine("Бот запущен");

await Task.Delay(-1);

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
{
    if (update.Message is { Text: { } messageText })
    {
        var chatId = update.Message.Chat.Id;

        if (messageText == "/start")
        {
            await bot.SendMessage(chatId, "Бот работает");
        }
    }
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
{
    Console.WriteLine(exception.Message);
    return Task.CompletedTask;
}
