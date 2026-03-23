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

        public MainViewModel()
        {
            _sqliteDataService.EnsureDatabaseCreated();

            var startOfWeek = GetStartOfWeek(DateTime.Today, DayOfWeek.Monday);

            for (int i = 0; i < 7; i++)
            {
                var currentDate = startOfWeek.AddDays(i);

                // UI label only. Persistence still uses DayIndex.
                var dayLabel = $"{currentDate:dddd} - {currentDate:dd}";

                var dayViewModel = new DayColumnViewModel(dayLabel, i);
                dayViewModel.DataChanged += OnDayDataChanged;

                Days.Add(dayViewModel);
            }

            LoadSavedData();
        }

        private void LoadSavedData()
        {
            _isLoadingData = true;

            try
            {
                var savedRows = _sqliteDataService.LoadAllDays();

                foreach (var day in Days)
                {
                    day.ClearAllTasks();

                    var rowsForDay = savedRows
                        .Where(x => x.DayIndex == day.DayIndex)
                        .OrderBy(x => x.DisplayOrder)
                        .ToList();

                    if (rowsForDay.Count == 0)
                    {
                        day.EnsureMinimumRows(10);
                        continue;
                    }

                    foreach (var row in rowsForDay)
                    {
                        var task = new TaskModel
                        {
                            Name = row.TaskName,
                            Points = row.Points
                        };

                        day.AddTask(task, row.IsDone);
                    }

                    day.EnsureMinimumRows(10);
                }
            }
            finally
            {
                _isLoadingData = false;
            }

            SaveAllDays();
        }

        private void OnDayDataChanged()
        {
            if (_isLoadingData || _isSavingData)
                return;

            SaveAllDays();
        }

        private void SaveAllDays()
        {
            if (_isLoadingData || _isSavingData)
                return;

            _isSavingData = true;

            try
            {
                _sqliteDataService.SaveAllDays(Days);
            }
            finally
            {
                _isSavingData = false;
            }
        }

        private static DateTime GetStartOfWeek(DateTime date, DayOfWeek startOfWeek)
        {
            int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.AddDays(-diff).Date;
        }
    }
}