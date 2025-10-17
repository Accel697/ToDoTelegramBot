using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ToDoBot.Model;

namespace ToDoBot.Services
{
    public class ToDoTelegram
    {
        private readonly ITelegramBotClient _botClient;
        private readonly Actions _actions;
        private readonly Dictionary<long, string> _userStates = new();

        public ToDoTelegram(string token)
        {
            _botClient = new TelegramBotClient(token);
            _actions = new Actions(new AppDbContext());
        }

        public async Task StartAsync()
        {
            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Бот запущен: {me.Username}");

            _botClient.StartReceiving(UpdateHandler, ErrorHandler);
            Console.WriteLine("Ожидание сообщений");
            await Task.Delay(-1);
        }

        private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message?.Text != null)
                {
                    await HandleMessage(update.Message);
                    Console.WriteLine($"{update.Message.From.FirstName} ({update.Message.From.Id}): {update.Message.Text}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {error.Message}");
            return Task.CompletedTask;
        }

        private async Task HandleMessage(Message message)
        {
            var userId = message.Chat.Id;
            var userInput = message.Text;

            var currentState = _userStates.ContainsKey(userId) ? _userStates[userId] : null;

            if (!string.IsNullOrEmpty(currentState))
            {
                await ProcessUserInput(userId, userInput, message.Chat.Id, currentState);
                return;
            }
            await ProcessCommand(userId, userInput, message.Chat.Id);
        }

        private async Task ProcessCommand(long userId, string command, long chatId)
        {
            switch (command)
            {
                case "/start":
                    await ShowMainMenu(chatId);
                    break;
                case "Список задач":
                    await ShowToDoList(userId, chatId);
                    break;
                case "Количество задач":
                    await ShowTaskCount(userId, chatId);
                    break;
                case "Добавить задачу":
                    _userStates[userId] = "ADD_TASK";
                    await _botClient.SendTextMessageAsync(chatId, "Введите название новой задачи:");
                    break;
                case "Найти задачу":
                    _userStates[userId] = "SEARCH_TASK";
                    await _botClient.SendTextMessageAsync(chatId, "Введите текст для поиска:");
                    break;
                case "Отметить выполненную":
                    _userStates[userId] = "MARK_DONE";
                    await _botClient.SendTextMessageAsync(chatId, "Введите ID задачи для отметки как выполненной:");
                    break;
                case "Отметить невыполненную":
                    _userStates[userId] = "MARK_UNDONE";
                    await _botClient.SendTextMessageAsync(chatId, "Введите ID задачи для отметки как невыполненной:");
                    break;
                case "Переименовать задачу":
                    _userStates[userId] = "RENAME_TASK";
                    await _botClient.SendTextMessageAsync(chatId, "Введите ID и новое название задачи в формате: \nID Новое название");
                    break;
                case "Удалить задачу":
                    _userStates[userId] = "DELETE_TASK";
                    await _botClient.SendTextMessageAsync(chatId, "Введите ID задачи для удаления:");
                    break;
                default:
                    await ShowMainMenu(chatId);
                    break;
            }
        }

        private async Task ProcessUserInput(long userId, string input, long chatId, string state)
        {
            _userStates.Remove(userId);

            try
            {
                switch (state)
                {
                    case "ADD_TASK":
                        await AddTask(userId, input, chatId);
                        break;
                    case "SEARCH_TASK":
                        await SearchTasks(userId, input, chatId);
                        break;
                    case "MARK_DONE":
                        await MarkTask(userId, input, chatId, true);
                        break;
                    case "MARK_UNDONE":
                        await MarkTask(userId, input, chatId, false);
                        break;
                    case "RENAME_TASK":
                        await RenameTask(userId, input, chatId);
                        break;
                    case "DELETE_TASK":
                        await DeleteTask(userId, input, chatId);
                        break;
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"Ошибка: {ex.Message}");
            }

            await ShowMainMenu(chatId);
        }

        private async Task ShowMainMenu(long chatId)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Список задач"), new KeyboardButton("Количество задач") },
                new[] { new KeyboardButton("Добавить задачу"), new KeyboardButton("Найти задачу") },
                new[] { new KeyboardButton("Отметить выполненную"), new KeyboardButton("Отметить невыполненную") },
                new[] { new KeyboardButton("Переименовать задачу"), new KeyboardButton("Удалить задачу") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            await _botClient.SendTextMessageAsync(chatId, "Выберите действие:", replyMarkup: replyKeyboard);
        }

        private async Task ShowToDoList(long userId, long chatId)
        {
            var todoList = await _actions.GetUserItems(userId);

            if (!todoList.Any())
            {
                await _botClient.SendTextMessageAsync(chatId, "Список задач пуст.");
                return;
            }

            var tasks = todoList.Select(item => $"{item.ItemId} {item.Title} - {(item.IsDone ? "Выполнена" : "Не выполнена")}");

            await _botClient.SendTextMessageAsync(chatId, "Ваши задачи:\n" + string.Join("\n", tasks));
        }

        private async Task ShowTaskCount(long userId, long chatId)
        {
            var todoList = await _actions.GetUserItems(userId);
            var total = todoList.Count;
            var done = todoList.Count(item => item.IsDone);
            var undone = total - done;

            await _botClient.SendTextMessageAsync(chatId, $"Всего: {total} \nВыполнено: {done} \nНе выполнено: {undone}");
        }

        private async Task AddTask(long userId, string title, long chatId)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                await _botClient.SendTextMessageAsync(chatId, "Название задачи не может быть пустым");
                return;
            }

            var newItem = await _actions.AddItem(userId, title);
            await _botClient.SendTextMessageAsync(chatId, $"Задача добавлена\n{newItem.ItemId} {title}");
        }

        private async Task SearchTasks(long userId, string searchText, long chatId)
        {
            var foundItems = await _actions.FindItems(userId, searchText);

            if (!foundItems.Any())
            {
                await _botClient.SendTextMessageAsync(chatId, "Задачи не найдены");
                return;
            }

            var result = "Результаты поиска:\n" + string.Join("\n", foundItems.Select(item => $"`{item.ItemId}` - {item.Title} {(item.IsDone ? "Выполнена" : "Не выполнена")}"));

            await _botClient.SendTextMessageAsync(chatId, result);
        }

        private async Task MarkTask(long userId, string input, long chatId, bool markAsDone)
        {
            if (!long.TryParse(input, out var taskId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Неверный формат ID");
                return;
            }

            bool success;
            if (markAsDone)
                success = await _actions.MarkAsDone(userId, taskId);
            else
                success = await _actions.MarkAsUndone(userId, taskId);

            if (!success)
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не найдена");
                return;
            }

            var status = markAsDone ? "выполнена" : "не выполнена";
            await _botClient.SendTextMessageAsync(chatId, $"Задача отмечена как {status}");
        }

        private async Task RenameTask(long userId, string input, long chatId)
        {
            var parts = input.Split(' ', 2);
            if (parts.Length != 2 || !long.TryParse(parts[0], out var taskId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Неверный формат. Используйте:\nID Новое название");
                return;
            }

            if (string.IsNullOrWhiteSpace(parts[1]))
            {
                await _botClient.SendTextMessageAsync(chatId, "Новое название не может быть пустым");
                return;
            }

            var success = await _actions.RenameItem(userId, taskId, parts[1]);

            if (!success)
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не найдена");
                return;
            }

            await _botClient.SendTextMessageAsync(chatId, "Задача переименована");
        }

        private async Task DeleteTask(long userId, string input, long chatId)
        {
            if (!long.TryParse(input, out var taskId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Неверный формат ID");
                return;
            }

            var success = await _actions.RemoveItem(userId, taskId);

            if (!success)
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не найдена");
                return;
            }

            await _botClient.SendTextMessageAsync(chatId, "Задача удалена");
        }
    }
}
