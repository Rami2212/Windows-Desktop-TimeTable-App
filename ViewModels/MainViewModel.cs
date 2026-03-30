using System;
using System.Collections.ObjectModel;
using System.Linq;
using TimeTableApp.Services;

namespace TimeTableApp.ViewModels
{
    using TaskModel = TimeTableApp.Models.Task;

    public class MainViewModel : BaseViewModel
    {
        private readonly SQLiteDataService _sqliteDataService = new SQLiteDataService();
        private bool _isLoadingData;
        private bool _isSavingData;

        public ObservableCollection<DayColumnViewModel> Days { get; } = new ObservableCollection<DayColumnViewModel>();

        public DayColumnViewModel ToDoColumn { get; }

        public WeeklyStatsViewModel WeeklyStats { get; } = new WeeklyStatsViewModel();

        public MainViewModel()
        {
            _sqliteDataService.EnsureDatabaseCreated();

            var startOfWeek = GetStartOfWeek(DateTime.Today, DayOfWeek.Monday);

            for (int i = 0; i < 7; i++)
            {
                var currentDate = startOfWeek.AddDays(i);
                var dayLabel = $"{currentDate:dddd} - {currentDate:dd}";
                var dayVm = new DayColumnViewModel(dayLabel, i);
                dayVm.DataChanged += OnDayDataChanged;
                Days.Add(dayVm);
            }

            ToDoColumn = new DayColumnViewModel("To Do", dayIndex: 7, isToDoColumn: true);
            ToDoColumn.DataChanged += OnDayDataChanged;

            // Wire up weekly-stats label saving
            WeeklyStats.LabelsChanged += SaveWeekLabels;

            LoadSavedData();
        }

        // ── Loading ──────────────────────────────────────────────────────────

        private void LoadSavedData()
        {
            _isLoadingData = true;

            try
            {
                var savedRows = _sqliteDataService.LoadAllDays();

                // Day columns
                foreach (var day in Days)
                {
                    day.ClearAllTasks();

                    var rowsForDay = savedRows
                        .Where(x => !x.IsToDoColumn && x.DayIndex == day.DayIndex)
                        .OrderBy(x => x.DisplayOrder)
                        .ToList();

                    if (rowsForDay.Count == 0)
                    {
                        day.EnsureMinimumRows(1);
                        continue;
                    }

                    foreach (var row in rowsForDay)
                    {
                        var task = new TaskModel { Name = row.TaskName, Points = row.Points };
                        day.AddTask(task, row.IsDone);
                    }
                }

                // To Do column
                ToDoColumn.ClearAllTasks();
                var toDoRows = savedRows
                    .Where(x => x.IsToDoColumn)
                    .OrderBy(x => x.DisplayOrder)
                    .ToList();

                if (toDoRows.Count == 0)
                    ToDoColumn.EnsureMinimumRows(1);
                else
                    foreach (var row in toDoRows)
                    {
                        var task = new TaskModel { Name = row.TaskName, Points = row.Points };
                        ToDoColumn.AddTask(task, row.IsDone);
                    }

                // Week labels
                var labels = _sqliteDataService.LoadWeekLabels();
                WeeklyStats.DayLabel = labels.TryGetValue("Day", out var d) ? d : string.Empty;
                WeeklyStats.WeekLabel = labels.TryGetValue("Week", out var w) ? w : string.Empty;
                WeeklyStats.MonthLabel = labels.TryGetValue("Month", out var m) ? m : string.Empty;
            }
            finally
            {
                _isLoadingData = false;
            }

            RefreshWeeklyStats();
            SaveAllDays();
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void OnDayDataChanged()
        {
            if (_isLoadingData || _isSavingData)
                return;

            RefreshWeeklyStats();
            SaveAllDays();
        }

        // ── Stats ────────────────────────────────────────────────────────────

        private void RefreshWeeklyStats()
        {
            int total = Days.Sum(d => d.TotalPossiblePoints);
            int completed = Days.Sum(d => d.TotalCompletedPoints);

            WeeklyStats.WeeklyTotalPoints = total;
            WeeklyStats.WeeklyCompletedPoints = completed;
        }

        // ── Persistence ──────────────────────────────────────────────────────

        private void SaveAllDays()
        {
            if (_isLoadingData || _isSavingData)
                return;

            _isSavingData = true;
            try
            {
                _sqliteDataService.SaveAllDays(Days, ToDoColumn);
            }
            finally
            {
                _isSavingData = false;
            }
        }

        private void SaveWeekLabels()
        {
            _sqliteDataService.SaveWeekLabels(
                WeeklyStats.DayLabel,
                WeeklyStats.WeekLabel,
                WeeklyStats.MonthLabel);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static DateTime GetStartOfWeek(DateTime date, DayOfWeek startOfWeek)
        {
            int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.AddDays(-diff).Date;
        }
    }
}