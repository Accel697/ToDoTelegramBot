using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        private readonly AppDbContext _context;
        private readonly Dictionary<long, string> _userStates = new();
        private readonly Dictionary<long, long> _userCurrentList = new();
        private readonly Dictionary<long, long> _userCurrentItem = new();

        public ToDoTelegram(string token)
        {
            _botClient = new TelegramBotClient(token);
            _context = new AppDbContext();
            _actions = new Actions(_context);
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
                    //Console.WriteLine($"{update.Message.From.FirstName} ({update.Message.From.Id}): {update.Message.Text}");
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

            await _actions.GetOrCreateUser(userId, message.From.FirstName);

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
                case "Мои листы":
                    await ShowUserLists(userId, chatId);
                    break;
                case "Создать лист":
                    _userStates[userId] = "CREATE_LIST";
                    await _botClient.SendTextMessageAsync(chatId, "Введите название нового листа:");
                    break;
                case "Количество листов":
                    await ShowListsCount(userId, chatId);
                    break;
                case "Назад к листам":
                    _userCurrentList.Remove(userId);
                    await ShowMainMenu(chatId);
                    break;
                case "Назад к задачам":
                    _userCurrentItem.Remove(userId);
                    await ShowListMenu(userId, chatId);
                    break;
                case "Список задач":
                    await ShowTasksList(userId, chatId);
                    break;
                case "Добавить задачу":
                    _userStates[userId] = "ADD_TASK";
                    await _botClient.SendTextMessageAsync(chatId, "Введите название новой задачи:");
                    break;
                case "Найти задачу":
                    _userStates[userId] = "SEARCH_TASK";
                    await _botClient.SendTextMessageAsync(chatId, "Введите текст для поиска:");
                    break;
                case "Переименовать лист":
                    _userStates[userId] = "RENAME_LIST";
                    await _botClient.SendTextMessageAsync(chatId, "Введите новое название листа:");
                    break;
                case "Удалить лист":
                    await DeleteCurrentList(userId, chatId);
                    break;
                case "Статистика листа":
                    await ShowListStats(userId, chatId);
                    break;
                case "Сменить статус":
                    await ShowStatusesMenu(userId, chatId);
                    break;
                case "Изменить название":
                    _userStates[userId] = "UPDATE_TASK_TITLE";
                    await _botClient.SendTextMessageAsync(chatId, "Введите новое название задачи:");
                    break;
                case "Изменить дату":
                    _userStates[userId] = "UPDATE_TASK_DATE";
                    await _botClient.SendTextMessageAsync(chatId, "Введите новую дату в формате ДД.ММ.ГГГГ:");
                    break;
                case "Изменить время":
                    _userStates[userId] = "UPDATE_TASK_TIME";
                    await _botClient.SendTextMessageAsync(chatId, "Введите новое время в формате ЧЧ:ММ:");
                    break;
                case "Добавить напоминание":
                    await ShowRemindersMenu(userId, chatId);
                    break;
                case "Мои напоминания":
                    await ShowItemReminders(userId, chatId);
                    break;
                case "Удалить задачу":
                    await DeleteCurrentItem(userId, chatId);
                    break;
                case "Напомнить за 15 мин":
                    await AddReminder15Min(userId, chatId);
                    break;
                case "Напомнить за 1 час":
                    await AddReminder1Hour(userId, chatId);
                    break;
                case "Напомнить за 1 день":
                    await AddReminder1Day(userId, chatId);
                    break;
                case "Другое напоминание":
                    _userStates[userId] = "ADD_REMINDER_CUSTOM";
                    await _botClient.SendTextMessageAsync(chatId, "Введите время в минутах (от 1 до 60):");
                    break;
                case "Назад к задаче":
                    await ShowItemMenu(userId, chatId);
                    break;
                default:
                    // Обработка выбора листа
                    if (command.StartsWith("/list_") && long.TryParse(command.Substring(6), out var listId))
                    {
                        await SelectList(userId, listId, chatId);
                        return;
                    }
                    // Обработка выбора задачи
                    if (command.StartsWith("/item_") && long.TryParse(command.Substring(6), out var itemId))
                    {
                        await SelectItem(userId, itemId, chatId);
                        return;
                    }
                    // Обработка смены статуса
                    if (command.StartsWith("status_") && long.TryParse(command.Substring(7).Split(' ')[0], out var statusId))
                    {
                        await ChangeItemStatus(userId, statusId, chatId);
                        return;
                    }
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
                    case "CREATE_LIST":
                        await CreateList(userId, input, chatId);
                        break;
                    case "RENAME_LIST":
                        await RenameList(userId, input, chatId);
                        break;
                    case "ADD_TASK":
                        await AddTask(userId, input, chatId);
                        break;
                    case "SEARCH_TASK":
                        await SearchTasks(userId, input, chatId);
                        break;
                    case "UPDATE_TASK_TITLE":
                        await UpdateTaskTitle(userId, input, chatId);
                        break;
                    case "UPDATE_TASK_DATE":
                        await UpdateTaskDate(userId, input, chatId);
                        break;
                    case "UPDATE_TASK_TIME":
                        await UpdateTaskTime(userId, input, chatId);
                        break;
                    case "ADD_REMINDER_CUSTOM":
                        await AddCustomReminder(userId, input, chatId);
                        break;
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"Ошибка: {ex.Message}");
            }
        }

        private async Task ShowMainMenu(long chatId)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Мои листы"), new KeyboardButton("Создать лист") },
                new[] { new KeyboardButton("Количество листов") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            await _botClient.SendTextMessageAsync(chatId, "Главное меню. Выберите действие:", replyMarkup: replyKeyboard);
        }

        private async Task ShowListMenu(long userId, long chatId)
        {
            if (!_userCurrentList.ContainsKey(userId))
            {
                await ShowMainMenu(chatId);
                return;
            }

            var list = await _context.Lists.FirstOrDefaultAsync(x => x.IdList == _userCurrentList[userId]);
            if (list == null)
            {
                await ShowMainMenu(chatId);
                return;
            }

            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Список задач"), new KeyboardButton("Добавить задачу") },
                new[] { new KeyboardButton("Найти задачу"), new KeyboardButton("Статистика листа") },
                new[] { new KeyboardButton("Переименовать лист"), new KeyboardButton("Удалить лист") },
                new[] { new KeyboardButton("Назад к листам") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            await _botClient.SendTextMessageAsync(chatId, $"Лист: {list.TitleList}\nВыберите действие:", replyMarkup: replyKeyboard);
        }

        private async Task ShowItemMenu(long userId, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await ShowListMenu(userId, chatId);
                return;
            }

            var item = await _actions.GetItemById(_userCurrentItem[userId]);
            if (item == null)
            {
                await ShowListMenu(userId, chatId);
                return;
            }

            var statuses = await _actions.GetStatuses();
            var currentStatus = statuses.FirstOrDefault(s => s.IdStatus == item.StatusItem);

            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Сменить статус"), new KeyboardButton("Изменить название") },
                new[] { new KeyboardButton("Изменить дату"), new KeyboardButton("Изменить время") },
                new[] { new KeyboardButton("Добавить напоминание"), new KeyboardButton("Мои напоминания") },
                new[] { new KeyboardButton("Удалить задачу"), new KeyboardButton("Назад к задачам") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            var message = $"Задача: {item.TitleItem}\n" +
                         $"Статус: {currentStatus?.TitleStatus}\n" +
                         $"Дата: {(item.DateItem.HasValue ? item.DateItem.Value.ToString("dd.MM.yyyy") : "Не установлена")}\n" +
                         $"Время: {(item.TimeItem.HasValue ? item.TimeItem.Value.ToString("HH:mm") : "Не установлено")}";

            await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: replyKeyboard);
        }

        private async Task ShowRemindersMenu(long userId, long chatId)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Напомнить за 15 мин"), new KeyboardButton("Напомнить за 1 час") },
                new[] { new KeyboardButton("Напомнить за 1 день"), new KeyboardButton("Другое напоминание") },
                new[] { new KeyboardButton("Назад к задаче") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            await _botClient.SendTextMessageAsync(chatId, "Выберите тип напоминания:", replyMarkup: replyKeyboard);
        }

        // Функции для работы с листами
        private async Task ShowUserLists(long userId, long chatId)
        {
            var lists = await _actions.GetUserLists(userId);

            if (!lists.Any())
            {
                await _botClient.SendTextMessageAsync(chatId, "У вас пока нет листов. Создайте первый лист!");
                return;
            }

            var message = "Ваши листы:\n" + string.Join("\n",
                lists.Select(list => $"/list_{list.IdList} - {list.TitleList}"));

            await _botClient.SendTextMessageAsync(chatId, message);
        }

        private async Task CreateList(long userId, string title, long chatId)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                await _botClient.SendTextMessageAsync(chatId, "Название листа не может быть пустым");
                return;
            }

            var newList = await _actions.CreateList(userId, title);
            await _botClient.SendTextMessageAsync(chatId, $"Лист создан: {newList.TitleList}");
            await ShowMainMenu(chatId);
        }

        private async Task SelectList(long userId, long listId, long chatId)
        {
            if (!await _actions.IsListOwner(listId, userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Лист не найден.");
                return;
            }

            _userCurrentList[userId] = listId;
            await ShowListMenu(userId, chatId);
        }

        private async Task RenameList(long userId, string newTitle, long chatId)
        {
            if (!_userCurrentList.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Лист не выбран");
                return;
            }

            if (string.IsNullOrWhiteSpace(newTitle))
            {
                await _botClient.SendTextMessageAsync(chatId, "Название не может быть пустым");
                return;
            }

            var success = await _actions.RenameList(_userCurrentList[userId], newTitle);
            if (!success)
            {
                await _botClient.SendTextMessageAsync(chatId, "Ошибка при переименовании");
                return;
            }

            await _botClient.SendTextMessageAsync(chatId, "Лист переименован");
            await ShowListMenu(userId, chatId);
        }

        private async Task DeleteCurrentList(long userId, long chatId)
        {
            if (!_userCurrentList.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Лист не выбран");
                return;
            }

            var success = await _actions.DeleteList(_userCurrentList[userId]);
            if (!success)
            {
                await _botClient.SendTextMessageAsync(chatId, "Ошибка при удалении");
                return;
            }

            _userCurrentList.Remove(userId);
            await _botClient.SendTextMessageAsync(chatId, "Лист удален");
            await ShowMainMenu(chatId);
        }

        private async Task ShowListsCount(long userId, long chatId)
        {
            var count = await _actions.GetUserListsCount(userId);
            await _botClient.SendTextMessageAsync(chatId, $"У вас {count} лист(ов)");
        }

        private async Task ShowListStats(long userId, long chatId)
        {
            if (!_userCurrentList.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Лист не выбран");
                return;
            }

            var count = await _actions.GetListItemsCount(_userCurrentList[userId]);
            await _botClient.SendTextMessageAsync(chatId, $"В листе {count} задач(а)");
        }

        // Функции для работы с задачами
        private async Task ShowTasksList(long userId, long chatId)
        {
            if (!_userCurrentList.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Лист не выбран");
                return;
            }

            var tasks = await _actions.GetTodayAndFutureItems(_userCurrentList[userId]);
            var statuses = await _actions.GetStatuses();

            if (!tasks.Any())
            {
                await _botClient.SendTextMessageAsync(chatId, "В листе нет задач на сегодня и будущее.");
                return;
            }

            var message = "Задачи в листе:\n" + string.Join("\n", tasks.Select(task =>
            {
                var status = statuses.FirstOrDefault(s => s.IdStatus == task.StatusItem);
                var dateInfo = task.DateItem.HasValue ? $" - {task.DateItem.Value:dd.MM.yyyy}" : "";
                var timeInfo = task.TimeItem.HasValue ? $" {task.TimeItem.Value:HH:mm}" : "";
                return $"/item_{task.IdItem} - {task.TitleItem} [{status?.TitleStatus}]{dateInfo}{timeInfo}";
            }));

            await _botClient.SendTextMessageAsync(chatId, message);
        }

        private async Task AddTask(long userId, string title, long chatId)
        {
            if (!_userCurrentList.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Лист не выбран");
                return;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                await _botClient.SendTextMessageAsync(chatId, "Название задачи не может быть пустым");
                return;
            }

            var newItem = await _actions.AddItem(_userCurrentList[userId], title);
            await _botClient.SendTextMessageAsync(chatId, $"Задача добавлена: {newItem.TitleItem}");
            await ShowListMenu(userId, chatId);
        }

        private async Task SearchTasks(long userId, string searchText, long chatId)
        {
            if (!_userCurrentList.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Лист не выбран");
                return;
            }

            var tasks = await _actions.GetTodayAndFutureItems(_userCurrentList[userId]);
            var foundTasks = tasks.Where(t => t.TitleItem.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
            var statuses = await _actions.GetStatuses();

            if (!foundTasks.Any())
            {
                await _botClient.SendTextMessageAsync(chatId, "Задачи не найдены");
                return;
            }

            var message = "Результаты поиска:\n" + string.Join("\n", foundTasks.Select(task =>
            {
                var status = statuses.FirstOrDefault(s => s.IdStatus == task.StatusItem);
                return $"/item_{task.IdItem} - {task.TitleItem} [{status?.TitleStatus}]";
            }));

            await _botClient.SendTextMessageAsync(chatId, message);
        }

        private async Task SelectItem(long userId, long itemId, long chatId)
        {
            var item = await _actions.GetItemById(itemId);
            if (item == null || !await _actions.IsListOwner(item.ListItem, userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не найдена.");
                return;
            }

            _userCurrentItem[userId] = itemId;
            await ShowItemMenu(userId, chatId);
        }

        private async Task ShowStatusesMenu(long userId, long chatId)
        {
            var statuses = await _actions.GetStatuses();
            var keyboard = statuses.Select(s => new[] { new KeyboardButton($"{s.TitleStatus}") }).ToList();
            keyboard.Add(new[] { new KeyboardButton("Назад к задаче") });

            var replyKeyboard = new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            await _botClient.SendTextMessageAsync(chatId, "Выберите новый статус:", replyMarkup: replyKeyboard);
        }

        private async Task ChangeItemStatus(long userId, long statusId, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не выбрана");
                return;
            }

            var success = await _actions.ChangeItemStatus(_userCurrentItem[userId], statusId);
            if (!success)
            {
                await _botClient.SendTextMessageAsync(chatId, "Ошибка при смене статуса");
                return;
            }

            var statuses = await _actions.GetStatuses();
            var newStatus = statuses.FirstOrDefault(s => s.IdStatus == statusId);

            await _botClient.SendTextMessageAsync(chatId, $"Статус изменен на: {newStatus?.TitleStatus}");
            await ShowItemMenu(userId, chatId);
        }

        private async Task UpdateTaskTitle(long userId, string newTitle, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не выбрана");
                return;
            }

            if (string.IsNullOrWhiteSpace(newTitle))
            {
                await _botClient.SendTextMessageAsync(chatId, "Название не может быть пустым");
                return;
            }

            var success = await _actions.UpdateItem(_userCurrentItem[userId], newTitle: newTitle);
            if (!success)
            {
                await _botClient.SendTextMessageAsync(chatId, "Ошибка при изменении названия");
                return;
            }

            await _botClient.SendTextMessageAsync(chatId, "Название задачи обновлено");
            await ShowItemMenu(userId, chatId);
        }

        private async Task UpdateTaskDate(long userId, string dateInput, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не выбрана");
                return;
            }

            if (!DateOnly.TryParseExact(dateInput, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                await _botClient.SendTextMessageAsync(chatId, "Неверный формат даты. Используйте ДД.ММ.ГГГГ");
                return;
            }

            var success = await _actions.UpdateItem(_userCurrentItem[userId], newDate: date);
            if (!success)
            {
                await _botClient.SendTextMessageAsync(chatId, "Ошибка при изменении даты");
                return;
            }

            await _botClient.SendTextMessageAsync(chatId, $"Дата задачи обновлена: {date:dd.MM.yyyy}");
            await ShowItemMenu(userId, chatId);
        }

        private async Task UpdateTaskTime(long userId, string timeInput, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не выбрана");
                return;
            }

            if (!TimeOnly.TryParseExact(timeInput, "HH:mm", null, System.Globalization.DateTimeStyles.None, out var time))
            {
                await _botClient.SendTextMessageAsync(chatId, "Неверный формат времени. Используйте ЧЧ:ММ");
                return;
            }

            var success = await _actions.UpdateItem(_userCurrentItem[userId], newTime: time);
            if (!success)
            {
                await _botClient.SendTextMessageAsync(chatId, "Ошибка при изменении времени");
                return;
            }

            await _botClient.SendTextMessageAsync(chatId, $"Время задачи обновлено: {time:HH:mm}");
            await ShowItemMenu(userId, chatId);
        }

        private async Task DeleteCurrentItem(long userId, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не выбрана");
                return;
            }

            var item = await _actions.GetItemById(_userCurrentItem[userId]);
            if (item == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не найдена");
                return;
            }

            var success = await _actions.DeleteItem(_userCurrentItem[userId]);
            if (!success)
            {
                await _botClient.SendTextMessageAsync(chatId, "Ошибка при удалении");
                return;
            }

            _userCurrentItem.Remove(userId);
            await _botClient.SendTextMessageAsync(chatId, "Задача удалена");
            await ShowListMenu(userId, chatId);
        }

        // Функции для работы с напоминаниями
        private async Task ShowItemReminders(long userId, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не выбрана");
                return;
            }

            var reminders = await _actions.GetItemReminders(_userCurrentItem[userId]);

            if (!reminders.Any())
            {
                await _botClient.SendTextMessageAsync(chatId, "У этой задачи нет напоминаний");
                return;
            }

            var message = "Напоминания для задачи:\n" + string.Join("\n",
                reminders.Select(r => $"{r.IdReminder} - {r.DateReminder:dd.MM.yyyy} {r.TimeReminder:HH:mm}"));

            await _botClient.SendTextMessageAsync(chatId, message);
            await ShowItemMenu(userId, chatId);
        }

        private async Task AddReminder15Min(long userId, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не выбрана");
                return;
            }

            try
            {
                var reminder = await _actions.AddReminder15Min(_userCurrentItem[userId]);
                await _botClient.SendTextMessageAsync(chatId, $"Напоминание установлено на: {reminder.DateReminder:dd.MM.yyyy} {reminder.TimeReminder:HH:mm}");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"Ошибка: {ex.Message}");
            }

            await ShowItemMenu(userId, chatId);
        }

        private async Task AddReminder1Hour(long userId, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не выбрана");
                return;
            }

            try
            {
                var reminder = await _actions.AddReminder1Hour(_userCurrentItem[userId]);
                await _botClient.SendTextMessageAsync(chatId, $"Напоминание установлено на: {reminder.DateReminder:dd.MM.yyyy} {reminder.TimeReminder:HH:mm}");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"Ошибка: {ex.Message}");
            }

            await ShowItemMenu(userId, chatId);
        }

        private async Task AddReminder1Day(long userId, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не выбрана");
                return;
            }

            try
            {
                var reminder = await _actions.AddReminder1Day(_userCurrentItem[userId]);
                await _botClient.SendTextMessageAsync(chatId, $"Напоминание установлено на: {reminder.DateReminder:dd.MM.yyyy} {reminder.TimeReminder:HH:mm}");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"Ошибка: {ex.Message}");
            }

            await ShowItemMenu(userId, chatId);
        }

        private async Task AddCustomReminder(long userId, string minutesInput, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не выбрана");
                return;
            }

            if (!int.TryParse(minutesInput, out var minutes) || minutes < 1 || minutes > 60)
            {
                await _botClient.SendTextMessageAsync(chatId, "Неверный формат. Введите число от 1 до 60");
                return;
            }

            try
            {
                var reminder = await _actions.AddReminderInMinutes(_userCurrentItem[userId], minutes);
                await _botClient.SendTextMessageAsync(chatId, $"Напоминание установлено на: {reminder.DateReminder:dd.MM.yyyy} {reminder.TimeReminder:HH:mm}");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"Ошибка: {ex.Message}");
            }

            await ShowItemMenu(userId, chatId);
        }

        private async Task RemoveReminder(long userId, string reminderIdInput, long chatId)
        {
            if (!_userCurrentItem.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Задача не выбрана");
                return;
            }

            if (!long.TryParse(reminderIdInput, out var reminderId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Неверный формат ID напоминания");
                return;
            }

            // Проверяем, что напоминание принадлежит текущей задаче
            var reminders = await _actions.GetItemReminders(_userCurrentItem[userId]);
            if (!reminders.Any(r => r.IdReminder == reminderId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Напоминание не найдено для этой задачи");
                return;
            }

            var success = await _actions.RemoveReminder(reminderId);
            if (!success)
            {
                await _botClient.SendTextMessageAsync(chatId, "Ошибка при удалении напоминания");
                return;
            }

            await _botClient.SendTextMessageAsync(chatId, "Напоминание удалено");
            await ShowItemMenu(userId, chatId);
        }
    }
}
