using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ToDoBot.Model;

namespace ToDoBot.Services
{
    public class Actions
    {
        private readonly AppDbContext _context;

        public Actions(AppDbContext context)
        {
            _context = context;
        }

        // Работа с пользователями
        public async Task<User> GetOrCreateUser(long userId, string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.IdUser == userId);
            if (user == null)
            {
                user = new User { IdUser = userId, Name = userName.Trim() };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            return user;
        }

        // Работа с листами
        public async Task<List<List>> GetUserLists(long userId)
        {
            return await _context.Lists.Where(x => x.UserList == userId).OrderBy(x => x.IdList).ToListAsync();
        }

        public async Task<List> CreateList(long userId, string title)
        {
            var newList = new List
            {
                TitleList = title.Trim(),
                UserList = userId
            };

            _context.Lists.Add(newList);
            await _context.SaveChangesAsync();
            return newList;
        }

        public async Task<bool> RenameList(long listId, string newTitle)
        {
            var list = await _context.Lists.FirstOrDefaultAsync(x => x.IdList == listId);
            if (list == null) return false;

            list.TitleList = newTitle.Trim();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteList(long listId)
        {
            var list = await _context.Lists.FirstOrDefaultAsync(l => l.IdList == listId);
            if (list == null) return false;

            var items = await _context.Items.Where(i => i.ListItem == listId).ToListAsync();

            if (items.Any())
            {
                foreach (var item in items)
                {
                    await DeleteItem(item.IdItem);
                }
            }

            _context.Lists.Remove(list);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUserListsCount(long userId)
        {
            return await _context.Lists.CountAsync(x => x.UserList == userId);
        }

        // Работа с задачами
        public async Task<List<Item>> GetTodayAndFutureItems(long listId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return await _context.Items
                .Where(x => x.ListItem == listId && (!x.DateItem.HasValue || x.DateItem >= today))
                .OrderBy(x => x.DateItem)
                .ThenBy(x => x.TimeItem)
                .ToListAsync();
        }

        public async Task<Item> AddItem(long listId, string title, DateOnly? date = null, TimeOnly? time = null)
        {
            var newItem = new Item
            {
                TitleItem = title.Trim(),
                StatusItem = 1, // Запланировано
                ListItem = listId,
                DateItem = date,
                TimeItem = time
            };

            _context.Items.Add(newItem);
            await _context.SaveChangesAsync();
            return newItem;
        }

        public async Task<bool> UpdateItem(long itemId, string? newTitle = null, DateOnly? newDate = null, TimeOnly? newTime = null)
        {
            var item = await _context.Items.FirstOrDefaultAsync(x => x.IdItem == itemId);
            if (item == null) return false;

            if (newTitle != null) item.TitleItem = newTitle.Trim();
            if (newDate.HasValue) item.DateItem = newDate;
            if (newTime.HasValue) item.TimeItem = newTime;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeItemStatus(long itemId, long newStatusId)
        {
            var item = await _context.Items.FirstOrDefaultAsync(x => x.IdItem == itemId);
            if (item == null) return false;

            item.StatusItem = newStatusId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteItem(long itemId)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.IdItem == itemId);
            if (item == null) return false;

            var reminders = await _context.Reminders.Where(r => r.ItemReminder == itemId).ToListAsync();

            if (reminders.Any())
            {
                foreach (var reminder in reminders)
                {
                    await RemoveReminder(reminder.IdReminder);
                }
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetListItemsCount(long listId)
        {
            return await _context.Items.CountAsync(x => x.ListItem == listId);
        }

        public async Task<Item?> GetItemById(long itemId)
        {
            return await _context.Items.FirstOrDefaultAsync(x => x.IdItem == itemId);
        }

        // Работа с напоминаниями
        public async Task<Reminder> AddReminderBeforeTask(long itemId, TimeSpan timeBefore)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.IdItem == itemId);
            if (item == null || !item.DateItem.HasValue || !item.TimeItem.HasValue)
                throw new Exception("У задачи не установлены дата и время");

            // Вычисляем время напоминания от времени задачи
            var taskDateTime = item.DateItem.Value.ToDateTime(item.TimeItem.Value);
            var reminderDateTime = taskDateTime - timeBefore;

            if (reminderDateTime < DateTime.Now)
                throw new Exception("Напоминание не может быть установлено в прошлом");

            var reminder = new Reminder
            {
                ItemReminder = itemId,
                DateReminder = DateOnly.FromDateTime(reminderDateTime),
                TimeReminder = TimeOnly.FromDateTime(reminderDateTime)
            };

            _context.Reminders.Add(reminder);
            await _context.SaveChangesAsync();
            return reminder;
        }

        public async Task<Reminder> AddReminder15Min(long itemId)
        {
            return await AddReminderBeforeTask(itemId, TimeSpan.FromMinutes(15));
        }

        public async Task<Reminder> AddReminder1Hour(long itemId)
        {
            return await AddReminderBeforeTask(itemId, TimeSpan.FromHours(1));
        }

        public async Task<Reminder> AddReminderInMinutes(long itemId, int minutes)
        {
            if (minutes < 1 || minutes > 60)
                throw new ArgumentException("Минуты должны быть от 1 до 60");

            return await AddReminderBeforeTask(itemId, TimeSpan.FromMinutes(minutes));
        }

        public async Task<bool> RemoveReminder(long reminderId)
        {
            var reminder = await _context.Reminders.FirstOrDefaultAsync(x => x.IdReminder == reminderId);
            if (reminder == null) return false;

            _context.Reminders.Remove(reminder);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Reminder>> GetItemReminders(long itemId)
        {
            return await _context.Reminders.Where(x => x.ItemReminder == itemId).OrderBy(x => x.DateReminder).ThenBy(x => x.TimeReminder).ToListAsync();
        }

        // Вспомогательные методы
        public async Task<List<ItemStatus>> GetStatuses()
        {
            return await _context.ItemStatuses.OrderBy(x => x.IdStatus).ToListAsync();
        }

        public async Task<bool> IsListOwner(long listId, long userId)
        {
            return await _context.Lists.AnyAsync(x => x.IdList == listId && x.UserList == userId);
        }
    }
}
