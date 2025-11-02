using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using ToDoBot.Model;

namespace ToDoBot.Services
{
    public class ReminderService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly AppDbContext _context;
        private Timer _reminderTimer;

        public ReminderService(string token, AppDbContext context)
        {
            _botClient = new TelegramBotClient(token);
            _context = context;
        }

        public void StartReminderService()
        {
            _reminderTimer = new Timer(async _ => await CheckAndSendRemindersAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            Console.WriteLine("Сервис напоминаний запущен");
        }

        private async Task CheckAndSendRemindersAsync()
        {
            try
            {
                var now = DateTime.Now;

                // Погрешность ±30 секунд
                var minTime = now.AddSeconds(-30);
                var maxTime = now.AddSeconds(30);

                var reminders = await _context.Reminders
                    .Where(r => r.DateReminder <= DateOnly.FromDateTime(now) && 
                    r.TimeReminder >= TimeOnly.FromDateTime(minTime) && 
                    r.TimeReminder <= TimeOnly.FromDateTime(maxTime))
                    .ToListAsync();
                

                if (reminders.Any())
                {
                    //Console.WriteLine($"Найдено напоминаний для отправки: {dueReminders.Count}");

                    foreach (var reminder in reminders)
                    {
                        await SendReminderAsync(reminder);
                        await RemoveReminderAsync(reminder.IdReminder);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке напоминаний: {ex.Message}");
            }
        }

        private async Task SendReminderAsync(Reminder reminder)
        {
            try
            {
                var item = await _context.Items.FirstOrDefaultAsync(i => i.IdItem == reminder.ItemReminder);
                if (item == null)
                {
                    Console.WriteLine($"Задача не найдена: {reminder.ItemReminder}");
                    return;
                }

                var list = await _context.Lists.FirstOrDefaultAsync(l => l.IdList == item.ListItem);
                if (list == null)
                {
                    Console.WriteLine($"Список не найден: {item.ListItem}");
                    return;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == list.UserList);
                if (user == null)
                {
                    Console.WriteLine($"Пользователь не найден: {list.UserList}");
                    return;
                }

                var message = $"НАПОМИНАНИЕ\n\n" +
                    $"Лист - {list.TitleList}\n" +
                    $"Задача - {item.TitleItem}\n" +
                    $"Время - {item.DateItem:dd.MM.yyyy} {item.TimeItem:HH:mm}";
                await _botClient.SendTextMessageAsync(chatId: user.IdUser, text: message, parseMode: ParseMode.Markdown);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке напоминания: {ex.Message}");
            }
        }

        private async Task RemoveReminderAsync(long reminderId)
        {
            try
            {
                var reminder = await _context.Reminders.FirstOrDefaultAsync(r => r.IdReminder == reminderId);

                if (reminder != null)
                {
                    _context.Reminders.Remove(reminder);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении напоминания: {ex.Message}");
            }
        }

        public void StopReminderService()
        {
            _reminderTimer?.Dispose();
            Console.WriteLine("Сервис напоминаний остановлен");
        }
    }
}
