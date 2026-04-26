using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net;
using System.Text;

var token = Environment.GetEnvironmentVariable("BOT_TOKEN");

if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("NO TOKEN");
    return;
}

var bot = new TelegramBotClient(token);

// ===== пользователи
var users = new Dictionary<long, string>(); // режим

// ===== источники (по умолчанию)
var defaultSources = new List<string>
{
    "https://sochi.com/news/",
    "https://rosseti-kuban.ru/"
};

// ===== источники пользователей
var userSources = new Dictionary<long, List<string>>();

var http = new HttpClient();
var lastData = new Dictionary<string, string>();

// ===== Render фикс (порт)
_ = Task.Run(() =>
{
    var listener = new HttpListener();
    listener.Prefixes.Add("http://*:10000/");
    listener.Start();

    while (true)
    {
        var ctx = listener.GetContext();
        var res = ctx.Response;
        var buf = Encoding.UTF8.GetBytes("ok");
        res.OutputStream.Write(buf, 0, buf.Length);
        res.Close();
    }
});

// ===== ПАРСИНГ САЙТОВ
_ = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            foreach (var user in userSources)
            {
                var id = user.Key;

                foreach (var source in user.Value)
                {
                    if (!source.StartsWith("http")) continue;

                    var html = await http.GetStringAsync(source);

                    if (!lastData.ContainsKey(source) || lastData[source] != html)
                    {
                        lastData[source] = html;

                        if (users.ContainsKey(id) && users[id] == "off")
                            continue;

                        if (users.ContainsKey(id) && users[id] == "communal" && !IsCommunal(html))
                            continue;

                        await bot.SendTextMessageAsync(id,
                            "🌐 <b>Новое обновление с сайта</b>\n\n" +
                            "📄 Краткое содержание:\n" +
                            html.Substring(0, Math.Min(500, html.Length)));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        await Task.Delay(60000);
    }
});

// ===== TELEGRAM
using var cts = new CancellationTokenSource();

bot.StartReceiving(
    HandleUpdate,
    HandleError,
    new ReceiverOptions(),
    cancellationToken: cts.Token
);

Console.WriteLine("BOT STARTED");

await Task.Delay(-1);

// ===== ОБРАБОТКА
async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken ct)
{
    // 📢 КАНАЛ
    if (update.ChannelPost is { Text: { } text })
    {
        foreach (var user in users)
        {
            if (user.Value == "off") continue;

            if (user.Value == "communal" && !IsCommunal(text))
                continue;

            await bot.SendTextMessageAsync(user.Key,
                "📢 <b>Новое сообщение из канала</b>\n\n" +
                text);
        }
        return;
    }

    // 👤 ПОЛЬЗОВАТЕЛЬ
    if (update.Message is { Text: { } msg })
    {
        var id = update.Message.Chat.Id;

        // если первый раз
        if (!userSources.ContainsKey(id))
            userSources[id] = new List<string>(defaultSources);

        if (!users.ContainsKey(id))
            users[id] = "all";

        if (msg == "/start")
        {
            var kb = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("📢 Все") },
                new[] { new KeyboardButton("🚿 Коммуналка") },
                new[] { new KeyboardButton("➕ Добавить источник") },
                new[] { new KeyboardButton("📂 Мои источники") },
                new[] { new KeyboardButton("❌ Выкл") }
            })
            { ResizeKeyboard = true };

            await bot.SendTextMessageAsync(id,
                "👋 <b>Привет!</b>\n\n" +
                "Я буду присылать тебе уведомления 📨\n\n" +
                "Выбери режим работы:");
        }
        else if (msg == "📢 Все")
        {
            users[id] = "all";
            await bot.SendTextMessageAsync(id,
                "📢 <b>Включены все уведомления</b>\n" +
                "Ты будешь получать всё подряд");
        }
        else if (msg == "🚿 Коммуналка")
        {
            users[id] = "communal";
            await bot.SendTextMessageAsync(id,
                "🚿 <b>Режим коммуналки включён</b>\n\n" +
                "Будут приходить только сообщения про:\n" +
                "💧 воду\n⚡ свет\n🔥 газ\n🛠 отключения");
        }
        else if (msg == "❌ Выкл")
        {
            users[id] = "off";
            await bot.SendTextMessageAsync(id,
                "❌ <b>Уведомления отключены</b>\n\n" +
                "Если захочешь вернуть — просто выбери режим");
        }
        else if (msg == "➕ Добавить источник")
        {
            await bot.SendTextMessageAsync(id,
                "➕ <b>Добавление источника</b>\n\n" +
                "Отправь ссылку:\n" +
                "🌐 сайт (https://...)\n" +
                "📢 Telegram-канал");
        }
        else if (msg.StartsWith("http"))
        {
            userSources[id].Add(msg);
            await bot.SendTextMessageAsync(id,
                "✅ <b>Источник добавлен!</b>\n" +
                "Теперь я буду его отслеживать");
        }
        else if (msg == "📂 Мои источники")
        {
            var list = string.Join("\n", userSources[id]);

            if (string.IsNullOrEmpty(list))
                list = "📭 Список пуст";

            await bot.SendTextMessageAsync(id,
                "📂 <b>Твои источники:</b>\n\n" + list);
        }
    }
}

// ===== ФИЛЬТР
bool IsCommunal(string text)
{
    var words = new[]
    {
        "вода",
        "свет",
        "газ",
        "отключ",
        "ремонт",
        "авар"
    };

    return words.Any(w => text.ToLower().Contains(w));
}

// ===== ОШИБКИ
Task HandleError(ITelegramBotClient bot, Exception ex, CancellationToken ct)
{
    Console.WriteLine(ex.Message);
    return Task.CompletedTask;
}
