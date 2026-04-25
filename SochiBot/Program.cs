using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Bot is running");
app.RunAsync(); // важно

var token = Environment.GetEnvironmentVariable("BOT_TOKEN");

if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("TOKEN NOT FOUND");
    return;
}

var bot = new TelegramBotClient(token);

using var cts = new CancellationTokenSource();

bot.StartReceiving(
    async (botClient, update, ct) =>
    {
        if (update.Type != UpdateType.Message || update.Message!.Text == null)
            return;

        var chatId = update.Message.Chat.Id;
        var text = update.Message.Text;

        if (text == "/start")
        {
            await botClient.SendMessage(chatId, "Бот работает ✅");
        }
        else if (text == "/help")
        {
            await botClient.SendMessage(chatId, "/start - запуск\n/help - помощь");
        }
        else
        {
            await botClient.SendMessage(chatId, "Не понимаю команду");
        }
    },
    async (botClient, exception, ct) =>
    {
        Console.WriteLine(exception.Message);
    },
    cancellationToken: cts.Token
);

Console.WriteLine("Bot started");

await Task.Delay(-1);
