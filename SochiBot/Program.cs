using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net;
using System.Text;

var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");

if (string.IsNullOrEmpty(botToken))
{
    Console.WriteLine("Token not found");
    return;
}

var bot = new TelegramBotClient(botToken);

// хранение режима пользователя
var usersMode = new Dictionary<long, string>();

// мини сервер для Render (чтобы не ругался на порт)
_ = Task.Run(() =>
{
    var listener = new HttpListener();
    listener.Prefixes.Add("http://*:10000/");
    listener.Start();

    while (true)
    {
        var ctx = listener.GetContext();
        var response = ctx.Response;
        var buffer = Encoding.UTF8.GetBytes("Bot is running");
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();
    }
});

using var cts = new CancellationTokenSource();

bot.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    new ReceiverOptions(),
    cancellationToken: cts.Token
);

Console.WriteLine("Бот запущен");

await Task.Delay(-1);

// =========================
// ОБРАБОТКА СООБЩЕНИЙ
// =========================

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

// =========================
// ФИЛЬТР КОММУНАЛКИ
// =========================

bool IsCommunal(string text)
{
    var keywords = new[]
    {
        "вода",
        "свет",
        "газ",
        "отключение",
        "ремонт",
        "коммун"
    };

    return keywords.Any(k => text.ToLower().Contains(k));
}

// =========================

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
{
    Console.WriteLine(exception.Message);
    return Task.CompletedTask;
}
