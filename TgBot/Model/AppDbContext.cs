using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

namespace ToDoBot.Model
{
    public class AppDbContext : DbContext
    {
        public DbSet<ToDoListItem> ToDoListItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Env.Load();
            var host = Env.GetString("Host");
            var port = Env.GetString("Port");
            var database = Env.GetString("Database");
            var username = Env.GetString("Username");
            var password = Env.GetString("Password");
            optionsBuilder.UseNpgsql($"Host={host};Port={port};Database={database};Username={username};Password={password}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ToDoListItem>(entity =>
            {
                entity.ToTable("to_do_list_item"); 

                entity.HasKey(e => new { e.UserId, e.ItemId });

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.Property(e => e.ItemId).HasColumnName("item_id");

                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);

                entity.Property(e => e.IsDone).HasColumnName("is_done");
            });
        }
    }
}
