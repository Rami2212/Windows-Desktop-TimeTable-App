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

            // Ensure new table exists
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS PersistedWeekLabels (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    LabelKey TEXT NOT NULL UNIQUE,
                    LabelValue TEXT NOT NULL
                )");
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
                    IsToDoColumn = true
                });
            }

            db.PersistedTaskItems.AddRange(rowsToSave);
            db.SaveChanges();
        }
    }
}