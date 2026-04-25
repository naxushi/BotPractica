using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

SQLitePCL.Batteries.Init();

var token = Environment.GetEnvironmentVariable("BOT_TOKEN"); 
var bot = new TelegramBotClient(token);

var db = new Db("Data Source=sochi.db");
db.Init();

var cts = new CancellationTokenSource();

bot.StartReceiving(
    async (botClient, update, ct) =>
    {
        if (update.Message is not { Text: { } text }) return;

        var userId = update.Message.Chat.Id;

        if (text == "/start")
        {
            db.AddUser(userId);
            await botClient.SendMessage(userId, "Ты подписан ✅");
        }
        else if (text.StartsWith("/add "))
        {
            var url = text.Replace("/add ", "");
            db.AddSource(url, "user");
            await botClient.SendMessage(userId, "Источник добавлен");
        }
        else if (text == "/list")
        {
            var sources = db.GetSources();
            var msg = string.Join("\n", sources.Select(s => s.Channel));
            await botClient.SendMessage(userId, msg);
        }
    },
    (botClient, ex, ct) =>
    {
        Console.WriteLine(ex.Message);
        return Task.CompletedTask;
    },
    cancellationToken: cts.Token
);

Console.WriteLine("Бот запущен");


_ = Task.Run(async () =>
{
    while (true)
    {
        var sources = db.GetSources();

        foreach (var s in sources)
        {
            var posts = await Parser.Parse(s.Channel);

            foreach (var post in posts)
            {
                foreach (var user in db.GetUsers())
                {
                    await bot.SendMessage(user, post);
                }
            }
        }

        await Task.Delay(60000);
    }
});

Console.ReadLine();
<<<<<<< Updated upstream
=======

await Task.Delay(-1);
>>>>>>> Stashed changes
