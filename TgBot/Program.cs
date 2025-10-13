using ToDoBot;

class Program
{
    static async Task Main(string[] args)
    {
        string botToken = "8367951186:AAETH3iVQrmrsU6Xi-34YszXy1CyrYJH0eM";

        var bot = new ToDoTelegram(botToken);
        await bot.StartAsync();
    }
}