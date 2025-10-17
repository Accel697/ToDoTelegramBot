using ToDoBot;
using DotNetEnv;
using ToDoBot.Services;

class Program
{
    static async Task Main(string[] args)
    {
        Env.Load();
        var botToken = Env.GetString("BotToken");

        if (botToken == null)
        {
            Console.WriteLine("Токен не найден");
        }
        else
        {
            var bot = new ToDoTelegram(botToken);
            await bot.StartAsync();
        }
    }
}