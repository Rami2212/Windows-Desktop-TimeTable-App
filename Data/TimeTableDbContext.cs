using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using TimeTableApp.Models;

namespace TimeTableApp.Data
{
    public class TimeTableDbContext : DbContext
    {
        public DbSet<PersistedTaskItem> PersistedTaskItems => Set<PersistedTaskItem>();
        public DbSet<PersistedWeekLabel> PersistedWeekLabels => Set<PersistedWeekLabel>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
                return;

            var appFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TimeTableApp");

            Directory.CreateDirectory(appFolder);

            var databasePath = Path.Combine(appFolder, "timetable.db");

            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PersistedTaskItem>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.DayIndex, x.DisplayOrder });
                entity.Property(x => x.TaskName).IsRequired();
            });

            modelBuilder.Entity<PersistedWeekLabel>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.LabelKey).IsUnique();
                entity.Property(x => x.LabelKey).IsRequired();
                entity.Property(x => x.LabelValue).IsRequired();
            });
        }
    }
}