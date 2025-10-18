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

        public async Task<List<ToDoListItem>> GetUserItems(long userId)
        {
            return await _context.ToDoListItems.Where(x => x.UserId == userId).OrderBy(x => x.ItemId).ToListAsync();
        }

        public async Task<ToDoListItem> AddItem(long userId, string title)
        {
            var maxItemId = await _context.ToDoListItems.AsNoTracking().Where(x => x.UserId == userId).MaxAsync(x => (long?)x.ItemId) ?? 0;

            var newItem = new ToDoListItem
            {
                UserId = userId,
                ItemId = maxItemId + 1,
                Title = title.Trim(),
                IsDone = false
            };

            _context.ToDoListItems.Add(newItem);

            await _context.SaveChangesAsync();

            return newItem;
        }

        public async Task<bool> RemoveItem(long userId, long itemId)
        {
            var item = await _context.ToDoListItems.FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);

            if (item == null) return false;

            _context.ToDoListItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ToDoListItem>> FindItems(long userId, string substring)
        {
            return await _context.ToDoListItems.Where(x => x.UserId == userId && x.Title.Contains(substring ?? string.Empty)).OrderBy(x => x.ItemId).ToListAsync();
        }

        public async Task<bool> MarkAsDone(long userId, long itemId)
        {
            var item = await _context.ToDoListItems.FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);

            if (item == null) return false;

            item.IsDone = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsUndone(long userId, long itemId)
        {
            var item = await _context.ToDoListItems.FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);

            if (item == null) return false;

            item.IsDone = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RenameItem(long userId, long itemId, string newTitle)
        {
            if (string.IsNullOrWhiteSpace(newTitle))
                return false;

            var item = await _context.ToDoListItems.FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);

            if (item == null) return false;

            item.Title = newTitle.Trim();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ToDoListItem?> GetItemById(long userId, long itemId)
        {
            return await _context.ToDoListItems.FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);
        }

        public async Task<int> GetItemsCount(long userId)
        {
            return await _context.ToDoListItems.CountAsync(x => x.UserId == userId);
        }
    }
}
