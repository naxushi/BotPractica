using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Bot is running");

app.RunAsync();

SQLitePCL.Batteries.Init();

var token = Environment.GetEnvironmentVariable("BOT_TOKEN");

if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("TOKEN NOT FOUND");
    return;
}
var bot = new TelegramBotClient(token);

var db = new Db();
DbInitializer.Init(db);

Console.WriteLine("Бот запущен");

var cts = new CancellationTokenSource();

bot.StartReceiving(
    async (botClient, update, ct) =>
    {
        if (update.Type == UpdateType.Message && update.Message.Text != null)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text;

            if (text == "/start")
            {
                db.AddUser(chatId);
                await botClient.SendMessage(chatId, "Ты подписан на уведомления ЖКХ ✅");
            }

            if (text == "/stop")
            {
                db.RemoveUser(chatId);
                await botClient.SendMessage(chatId, "Ты отписался ❌");
            }
        }
    },
    async (botClient, exception, ct) =>
    {
        Console.WriteLine(exception.Message);
    },
    new ReceiverOptions { AllowedUpdates = { } },
    cancellationToken: cts.Token
);

// запуск парсера
_ = Task.Run(async () =>
{
    while (true)
    {
        var posts = await Parser.GetPosts();

        foreach (var post in posts)
        {
            if (!Filter.IsRelevant(post))
                continue;

            if (DbInitializer.IsDuplicate(post.Id))
                continue;

            DbInitializer.SavePost(post.Id);

            foreach (var user in db.GetUsers())
            {
                await bot.SendMessage(user, post.Text);
            }
        }

        await Task.Delay(60000);
    }
});

await Task.Delay(-1);
