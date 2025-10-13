using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDo.Core
{
    public class ToDoItem
    {
        private static long _id = 0;

        public long Id { get; } = _id += 1;

        public string Title { get; private set; }

        public bool IsDone { get; private set; }

        public ToDoItem(string title)
        {
            Title = title?.Trim() ?? throw new ArgumentNullException(nameof(title));
        }

        public void MarkDone() => IsDone = true;

        public void MarkUndone() => IsDone = false;

        public void Rename(string newTitle)
        {
            if (string.IsNullOrWhiteSpace(newTitle)) throw new ArgumentException("Title required", nameof(newTitle));

            Title = newTitle.Trim();
        }
    }
}
