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
            var server = Env.GetString("Server");
            var database = Env.GetString("Database");
            optionsBuilder.UseSqlServer($"Server={server};Database={database};Trusted_Connection=true;TrustServerCertificate=true;");
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
