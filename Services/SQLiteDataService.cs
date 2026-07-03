using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using TimeTableApp.Data;
using TimeTableApp.Models;
using TimeTableApp.ViewModels;

namespace TimeTableApp.Services
{
    public class SQLiteDataService
    {
        public void EnsureDatabaseCreated()
        {
            using var db = new TimeTableDbContext();
            db.Database.EnsureCreated();

            // Existing column patch
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PersistedTaskItems ADD COLUMN IsToDoColumn INTEGER NOT NULL DEFAULT 0"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PersistedTaskItems ADD COLUMN IsPriority INTEGER NOT NULL DEFAULT 0"); } catch { }

            // Ensure new table exists
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS PersistedWeekLabels (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    LabelKey TEXT NOT NULL UNIQUE,
                    LabelValue TEXT NOT NULL
                )");

            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS PersistedWorkQueueCells (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RowIndex INTEGER NOT NULL,
                    ColumnIndex INTEGER NOT NULL,
                    Value TEXT NOT NULL DEFAULT '',
                    UNIQUE(RowIndex, ColumnIndex)
                )");

            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS PersistedDayTimers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DayIndex INTEGER NOT NULL UNIQUE,
                    ElapsedMilliseconds INTEGER NOT NULL DEFAULT 0,
                    IsRunning INTEGER NOT NULL DEFAULT 0,
                    RunningStartedUtc TEXT NULL
                )");
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PersistedDayTimers ADD COLUMN ElapsedMilliseconds INTEGER NOT NULL DEFAULT 0"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PersistedDayTimers ADD COLUMN IsRunning INTEGER NOT NULL DEFAULT 0"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PersistedDayTimers ADD COLUMN RunningStartedUtc TEXT NULL"); } catch { }
        }

        public List<PersistedTaskItem> LoadAllDays()
        {
            using var db = new TimeTableDbContext();

            return db.PersistedTaskItems
                .OrderBy(x => x.DayIndex)
                .ThenBy(x => x.DisplayOrder)
                .ToList();
        }

        public Dictionary<string, string> LoadWeekLabels()
        {
            using var db = new TimeTableDbContext();

            return db.PersistedWeekLabels
                .ToDictionary(x => x.LabelKey, x => x.LabelValue);
        }

        public List<PersistedWorkQueueCell> LoadWorkQueueCells()
        {
            using var db = new TimeTableDbContext();

            return db.PersistedWorkQueueCells
                .OrderBy(x => x.RowIndex)
                .ThenBy(x => x.ColumnIndex)
                .ToList();
        }

        public List<PersistedDayTimer> LoadDayTimers()
        {
            using var db = new TimeTableDbContext();

            return db.PersistedDayTimers
                .OrderBy(x => x.DayIndex)
                .ToList();
        }

        public void SaveWeekLabels(string dayLabel, string weekLabel, string monthLabel)
        {
            using var db = new TimeTableDbContext();

            var existing = db.PersistedWeekLabels.ToList();
            db.PersistedWeekLabels.RemoveRange(existing);

            db.PersistedWeekLabels.AddRange(
                new PersistedWeekLabel { LabelKey = "Day", LabelValue = dayLabel },
                new PersistedWeekLabel { LabelKey = "Week", LabelValue = weekLabel },
                new PersistedWeekLabel { LabelKey = "Month", LabelValue = monthLabel }
            );

            db.SaveChanges();
        }

        public void SaveAllDays(IEnumerable<DayColumnViewModel> days, DayColumnViewModel toDoColumn)
        {
            using var db = new TimeTableDbContext();

            var existingRows = db.PersistedTaskItems.ToList();

            if (existingRows.Count > 0)
                db.PersistedTaskItems.RemoveRange(existingRows);

            var rowsToSave = new List<PersistedTaskItem>();

            foreach (var day in days)
            {
                for (int i = 0; i < day.DayTasks.Count; i++)
                {
                    var row = day.DayTasks[i];
                    rowsToSave.Add(new PersistedTaskItem
                    {
                        DayIndex = day.DayIndex,
                        DisplayOrder = i,
                        TaskName = row.TaskName ?? string.Empty,
                        Points = row.Points,
                        IsDone = row.IsDone,
                        IsPriority = row.IsPriority,
                        IsToDoColumn = false
                    });
                }
            }

            for (int i = 0; i < toDoColumn.DayTasks.Count; i++)
            {
                var row = toDoColumn.DayTasks[i];
                rowsToSave.Add(new PersistedTaskItem
                {
                    DayIndex = 7,
                    DisplayOrder = i,
                    TaskName = row.TaskName ?? string.Empty,
                    Points = row.Points,
                    IsDone = row.IsDone,
                    IsPriority = row.IsPriority,
                    IsToDoColumn = true
                });
            }

            db.PersistedTaskItems.AddRange(rowsToSave);
            db.SaveChanges();
        }

        public void SaveWorkQueueCells(IEnumerable<WorkQueueCellViewModel> cells)
        {
            using var db = new TimeTableDbContext();

            var existingCells = db.PersistedWorkQueueCells.ToList();
            if (existingCells.Count > 0)
                db.PersistedWorkQueueCells.RemoveRange(existingCells);

            db.PersistedWorkQueueCells.AddRange(
                cells.Select(cell => new PersistedWorkQueueCell
                {
                    RowIndex = cell.RowIndex,
                    ColumnIndex = cell.ColumnIndex,
                    Value = cell.Value ?? string.Empty
                }));

            db.SaveChanges();
        }

        public void SaveDayTimers(IEnumerable<DayColumnViewModel> days)
        {
            using var db = new TimeTableDbContext();

            var existingTimers = db.PersistedDayTimers.ToList();
            if (existingTimers.Count > 0)
                db.PersistedDayTimers.RemoveRange(existingTimers);

            db.PersistedDayTimers.AddRange(
                days.Where(day => !day.IsToDoColumn)
                    .Select(day => new PersistedDayTimer
                    {
                        DayIndex = day.DayIndex,
                        ElapsedMilliseconds = day.StoredElapsedMilliseconds,
                        IsRunning = day.IsTimerRunning,
                        RunningStartedUtc = day.RunningStartedUtc
                    }));

            db.SaveChanges();
        }
    }
}
