using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

SQLitePCL.Batteries.Init();

var token = Environment.GetEnvironmentVariable("BOT_TOKEN");
var bot = new TelegramBotClient(token);
var db = new Db();
DbInitializer.Init(db);

var sourceRepo = new SourceRepository(db);
var subRepo = new SubscriptionRepository(db);
var userRepo = new UserRepository(db);

DbInitializer.Seed(sourceRepo);

var cts = new CancellationTokenSource();

bot.StartReceiving(async (client, update, token) =>
{
    if (update.Type != UpdateType.Message) return;
    var msg = update.Message;
    if (msg?.Text == null) return;

    var text = msg.Text;
    var userId = msg.Chat.Id;

    userRepo.AddIfNotExists(userId);

    if (text == "/start")
    {
        await client.SendMessage(userId,
            "🌴 Бот Сочи\n\n" +
            "/sources - источники\n" +
            "/sub {id} - подписка\n" +
            "/my - мои подписки\n" +
            "/addchannel - добавить канал");
    }

    else if (text == "/sources")
    {
        var sources = sourceRepo.GetAll();
        var response = string.Join("\n", sources.Select(s =>
            $"{s.Id}. {s.Name} [{s.Category}] (@{s.Channel})"));

        await client.SendMessage(userId, response);
    }

    else if (text.StartsWith("/sub"))
    {
        var parts = text.Split(' ');
        if (parts.Length < 2) return;

        int id = int.Parse(parts[1]);
        subRepo.Subscribe(userId, id);

        await client.SendMessage(userId, "✅ Подписка добавлена");
    }

    else if (text == "/my")
    {
        var subs = subRepo.GetUserSubscriptions(userId);
        await client.SendMessage(userId, "Ваши подписки: " + string.Join(", ", subs));
    }

    else if (text == "/addchannel")
    {
        await client.SendMessage(userId,
            "Отправь username канала (пример: sochinews)");
    }

    else if (!text.StartsWith("/"))
    {
        sourceRepo.Add(new Source
        {
            Name = text,
            Channel = text,
            Category = "user"
        });

        await client.SendMessage(userId, "Канал добавлен ✅");
    }

}, async (client, ex, token) =>
{
    Console.WriteLine(ex.Message);
}, cancellationToken: cts.Token);

// =========================
// ПРОСТОЙ ПАРСИНГ TELEGRAM КАНАЛОВ
// =========================

_ = Task.Run(async () =>
{
    while (true)
    {
        var sources = sourceRepo.GetAll();
        var users = userRepo.GetAll();

        foreach (var source in sources)
        {
            try
            {
                var url = $"https://t.me/s/{source.Channel}";
                var html = await new HttpClient().GetStringAsync(url);

                var posts = html.Split("tgme_widget_message_text");

                var latest = posts.Skip(1).Take(2)
                    .Select(p => ExtractText(p))
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();

                foreach (var user in users)
                {
                    var subs = subRepo.GetUserSubscriptions(user.Id);
                    if (!subs.Contains(source.Id)) continue;

                    foreach (var post in latest)
                    {
                        await bot.SendMessage(user.Id,
                            $"[{source.Name}]\n{post}");
                    }
                }
            }
            catch { }
        }

        await Task.Delay(TimeSpan.FromMinutes(5));
    }
});

Console.ReadLine();

string ExtractText(string html)
{
    var start = html.IndexOf(">");
    var end = html.IndexOf("</div>");

    if (start == -1 || end == -1) return "";

    return html.Substring(start + 1, end - start - 1)
        .Replace("<br>", "\n")
        .Replace("&quot;", "\"")
        .Trim();
}

// =========================
// DB
// =========================

public class Db
{
    private string cs = "Data Source=bot.db";
    public IDbConnection Create() => new SqliteConnection(cs);
}

public static class DbInitializer
{
    public static void Init(Db db)
    {
        using var c = db.Create();
        c.Execute(@"
        CREATE TABLE IF NOT EXISTS Users(Id INTEGER PRIMARY KEY);
        CREATE TABLE IF NOT EXISTS Sources(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Channel TEXT, Category TEXT);
        CREATE TABLE IF NOT EXISTS Subs(UserId INTEGER, SourceId INTEGER);
        ");
    }

    public static void Seed(SourceRepository repo)
    {
        if (repo.GetAll().Any()) return;

        repo.Add(new Source { Name = "Новости Сочи", Channel = "sochinews", Category = "news" });
        repo.Add(new Source { Name = "Типичный Сочи", Channel = "typical_sochi", Category = "city" });
    }
}

// =========================
// MODELS
// =========================

public class Source
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Channel { get; set; }
    public string Category { get; set; }
}

public class User
{
    public long Id { get; set; }
}

// =========================
// REPOS
// =========================

public class SourceRepository
{
    private Db db;
    public SourceRepository(Db db) => this.db = db;

    public IEnumerable<Source> GetAll()
    {
        using var c = db.Create();
        return c.Query<Source>("SELECT * FROM Sources");
    }

    public void Add(Source s)
    {
        using var c = db.Create();
        c.Execute("INSERT INTO Sources(Name,Channel,Category) VALUES(@Name,@Channel,@Category)", s);
    }
}

public class UserRepository
{
    private Db db;
    public UserRepository(Db db) => this.db = db;

    public void AddIfNotExists(long id)
    {
        using var c = db.Create();
        c.Execute("INSERT OR IGNORE INTO Users(Id) VALUES(@id)", new { id });
    }

    public IEnumerable<User> GetAll()
    {
        using var c = db.Create();
        return c.Query<User>("SELECT * FROM Users");
    }
}

public class SubscriptionRepository
{
    private Db db;
    public SubscriptionRepository(Db db) => this.db = db;

    public void Subscribe(long userId, int sourceId)
    {
        using var c = db.Create();
        c.Execute("INSERT INTO Subs(UserId,SourceId) VALUES(@userId,@sourceId)", new { userId, sourceId });
    }

    public List<int> GetUserSubscriptions(long userId)
    {
        using var c = db.Create();
        return c.Query<int>("SELECT SourceId FROM Subs WHERE UserId=@userId", new { userId }).ToList();
    }
}
