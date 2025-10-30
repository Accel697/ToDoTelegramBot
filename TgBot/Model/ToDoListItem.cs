using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoBot.Model
{
    public class User
    {
        public long IdUser { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class List
    {
        public long IdList { get; set; }
        public string TitleList { get; set; } = string.Empty;
        public long UserList { get; set; }
    }

    public class ItemStatus
    {
        public long IdStatus { get; set; }
        public string TitleStatus { get; set; } = string.Empty;
    }

    public class Item
    {
        public long IdItem { get; set; }
        public string TitleItem { get; set; } = string.Empty;
        public long StatusItem { get; set; }
        public long ListItem { get; set; }
        public DateOnly? DateItem { get; set; }
        public TimeOnly? TimeItem { get; set; }
    }

    public class Reminder
    {
        public long IdReminder { get; set; }
        public long ItemReminder { get; set; }
        public DateOnly DateReminder { get; set; }
        public TimeOnly TimeReminder { get; set; }
    }
}
