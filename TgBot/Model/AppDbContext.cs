using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace ToDoBot.Model
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<List> Lists { get; set; }
        public DbSet<ItemStatus> ItemStatuses { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

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
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.IdUser);
                entity.Property(e => e.IdUser).HasColumnName("id_user");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(60);
            });

            modelBuilder.Entity<List>(entity =>
            {
                entity.ToTable("list");
                entity.HasKey(e => e.IdList);
                entity.Property(e => e.IdList).HasColumnName("id_list");
                entity.Property(e => e.TitleList).HasColumnName("title_list").HasMaxLength(60);
                entity.Property(e => e.UserList).HasColumnName("user_list");

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserList)
                    .HasConstraintName("list_user_list_fkey");
            });

            modelBuilder.Entity<ItemStatus>(entity =>
            {
                entity.ToTable("item_status");
                entity.HasKey(e => e.IdStatus);
                entity.Property(e => e.IdStatus).HasColumnName("id_status");
                entity.Property(e => e.TitleStatus).HasColumnName("title_status").HasMaxLength(15);
            });

            modelBuilder.Entity<Item>(entity =>
            {
                entity.ToTable("item");
                entity.HasKey(e => e.IdItem);
                entity.Property(e => e.IdItem).HasColumnName("id_item");
                entity.Property(e => e.TitleItem).HasColumnName("title_item").HasMaxLength(200);
                entity.Property(e => e.StatusItem).HasColumnName("status_item");
                entity.Property(e => e.ListItem).HasColumnName("list_item");
                entity.Property(e => e.DateItem).HasColumnName("date_item");
                entity.Property(e => e.TimeItem).HasColumnName("time_item");

                entity.HasOne<ItemStatus>()
                    .WithMany()
                    .HasForeignKey(e => e.StatusItem)
                    .HasConstraintName("item_status_item_fkey");

                entity.HasOne<List>()
                    .WithMany()
                    .HasForeignKey(e => e.ListItem)
                    .HasConstraintName("item_list_item_fkey");
            });

            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.ToTable("reminder");
                entity.HasKey(e => e.IdReminder);
                entity.Property(e => e.IdReminder).HasColumnName("id_reminder");
                entity.Property(e => e.ItemReminder).HasColumnName("item_reminder");
                entity.Property(e => e.DateReminder).HasColumnName("date_reminder");
                entity.Property(e => e.TimeReminder).HasColumnName("time_reminder");

                entity.HasOne<Item>()
                    .WithMany()
                    .HasForeignKey(e => e.ItemReminder)
                    .HasConstraintName("reminder_item_reminder_fkey");
            });
        }
    }
}
