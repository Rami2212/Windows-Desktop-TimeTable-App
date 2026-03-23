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
        }

        public List<PersistedTaskItem> LoadAllDays()
        {
            using var db = new TimeTableDbContext();

            return db.PersistedTaskItems
                .OrderBy(x => x.DayIndex)
                .ThenBy(x => x.DisplayOrder)
                .ToList();
        }

        public void SaveAllDays(IEnumerable<DayColumnViewModel> days)
        {
            using var db = new TimeTableDbContext();

            var existingRows = db.PersistedTaskItems.ToList();

            if (existingRows.Count > 0)
            {
                db.PersistedTaskItems.RemoveRange(existingRows);
            }

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
                        IsDone = row.IsDone
                    });
                }
            }

            db.PersistedTaskItems.AddRange(rowsToSave);
            db.SaveChanges();
        }
    }
}