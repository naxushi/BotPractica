using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");

if (string.IsNullOrEmpty(botToken))
{
    Console.WriteLine("Token not found");
    return;
}

var bot = new TelegramBotClient(botToken);

var usersMode = new Dictionary<long, string>(); // хранит режим пользователя

using var cts = new CancellationTokenSource();

bot.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    new ReceiverOptions(),
    cancellationToken: cts.Token
);

Console.WriteLine("Бот запущен");

await Task.Delay(-1);


async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
{
    if (update.Message is { Text: { } text })
    {
        var chatId = update.Message.Chat.Id;

        if (text == "/start")
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("📢 Все новости") },
                new[] { new KeyboardButton("🚿 Только коммунальные") },
                new[] { new KeyboardButton("❌ Выключить") }
            })
            {
                ResizeKeyboard = true
            };

            await bot.SendTextMessageAsync(chatId,
                "Выбери режим:",
                replyMarkup: keyboard);

            return;
        }

        // выбор режима
        if (text == "📢 Все новости")
        {
            usersMode[chatId] = "all";
            await bot.SendTextMessageAsync(chatId, "Теперь будут приходить ВСЕ новости");
        }
        else if (text == "🚿 Только коммунальные")
        {
            usersMode[chatId] = "communal";
            await bot.SendTextMessageAsync(chatId, "Теперь только коммунальные уведомления");
        }
        else if (text == "❌ Выключить")
        {
            usersMode[chatId] = "off";
            await bot.SendTextMessageAsync(chatId, "Уведомления выключены");
        }
    }
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
{
    Console.WriteLine(exception.Message);
    return Task.CompletedTask;
}
