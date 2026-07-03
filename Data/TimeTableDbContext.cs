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
        public DbSet<PersistedWorkQueueCell> PersistedWorkQueueCells => Set<PersistedWorkQueueCell>();
        public DbSet<PersistedDayTimer> PersistedDayTimers => Set<PersistedDayTimer>();

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

            modelBuilder.Entity<PersistedWorkQueueCell>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.RowIndex, x.ColumnIndex }).IsUnique();
                entity.Property(x => x.Value).IsRequired();
            });

            modelBuilder.Entity<PersistedDayTimer>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.DayIndex).IsUnique();
            });
        }
    }
}
