using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Threading;
using Microsoft.Win32;
using TimeTableApp.Models;
using TimeTableApp.Services;

namespace TimeTableApp.ViewModels
{
    using TaskModel = TimeTableApp.Models.Task;

    public class MainViewModel : BaseViewModel
    {
        private readonly SQLiteDataService _sqliteDataService = new SQLiteDataService();
        private readonly DispatcherTimer _dayRefreshTimer;
        private bool _isLoadingData;
        private bool _isSavingData;

        public ObservableCollection<DayColumnViewModel> Days { get; } = new ObservableCollection<DayColumnViewModel>();

        public ObservableCollection<WorkQueueCellViewModel> WorkQueueCells { get; } = new ObservableCollection<WorkQueueCellViewModel>();

        public DayColumnViewModel ToDoColumn { get; }

        public WeeklyStatsViewModel WeeklyStats { get; } = new WeeklyStatsViewModel();

        public RelayCommand ExportJsonCommand { get; }

        public RelayCommand ImportJsonCommand { get; }

        public RelayCommand UndoLastRemovedTaskCommand { get; }

        public MainViewModel()
        {
            _sqliteDataService.EnsureDatabaseCreated();

            var startOfWeek = GetStartOfWeek(DateTime.Today, DayOfWeek.Monday);

            for (int i = 0; i < 7; i++)
            {
                var currentDate = startOfWeek.AddDays(i);
                var dayLabel = $"{currentDate:dddd} - {currentDate:dd}";
                var dayVm = new DayColumnViewModel(dayLabel, i, currentDate);
                dayVm.DataChanged += OnDayDataChanged;
                dayVm.TimerSaved += OnTimerSaved;
                dayVm.UndoStateChanged += OnUndoStateChanged;
                Days.Add(dayVm);
            }

            ToDoColumn = new DayColumnViewModel("To Do", dayIndex: 7, isToDoColumn: true);
            ToDoColumn.DataChanged += OnDayDataChanged;
            ToDoColumn.UndoStateChanged += OnUndoStateChanged;

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    var cell = new WorkQueueCellViewModel(row, column);
                    cell.PropertyChanged += OnWorkQueueCellPropertyChanged;
                    WorkQueueCells.Add(cell);
                }
            }

            ExportJsonCommand = new RelayCommand(ExportToJson);
            ImportJsonCommand = new RelayCommand(ImportFromJson);
            UndoLastRemovedTaskCommand = new RelayCommand(UndoLastRemovedTask, CanUndoLastRemovedTask);

            _dayRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _dayRefreshTimer.Tick += (_, _) => RefreshDayTimers();
            _dayRefreshTimer.Start();

            WeeklyStats.LabelsChanged += SaveWeekLabels;

            LoadSavedData();
        }

        private void LoadSavedData()
        {
            _isLoadingData = true;

            try
            {
                ApplyTasks(_sqliteDataService.LoadAllDays());
                ApplyWorkQueueCells(_sqliteDataService.LoadWorkQueueCells());
                ApplyDayTimers(_sqliteDataService.LoadDayTimers());

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
            RefreshDayTimers();
            SaveAllState();
        }

        private void OnDayDataChanged()
        {
            if (_isLoadingData || _isSavingData)
                return;

            RefreshWeeklyStats();
            UndoLastRemovedTaskCommand.RaiseCanExecuteChanged();
            SaveAllDays();
        }

        private void OnTimerSaved()
        {
            if (_isLoadingData || _isSavingData)
                return;

            SaveDayTimers();
        }

        private void OnUndoStateChanged()
        {
            UndoLastRemovedTaskCommand.RaiseCanExecuteChanged();
        }

        private void OnWorkQueueCellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(WorkQueueCellViewModel.Value) || _isLoadingData || _isSavingData)
                return;

            SaveWorkQueueCells();
        }

        private void RefreshWeeklyStats()
        {
            int total = Days.Sum(d => d.TotalPossiblePoints);
            int completed = Days.Sum(d => d.TotalCompletedPoints);

            WeeklyStats.WeeklyTotalPoints = total;
            WeeklyStats.WeeklyCompletedPoints = completed;
        }

        private void RefreshDayTimers()
        {
            foreach (var day in Days)
                day.RefreshTimerAvailability();
        }

        private void SaveAllState()
        {
            SaveAllDays();
            SaveWorkQueueCells();
            SaveDayTimers();
            SaveWeekLabels();
        }

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

        private void SaveWorkQueueCells()
        {
            if (_isLoadingData || _isSavingData)
                return;

            _isSavingData = true;
            try
            {
                _sqliteDataService.SaveWorkQueueCells(WorkQueueCells);
            }
            finally
            {
                _isSavingData = false;
            }
        }

        private void SaveDayTimers()
        {
            if (_isLoadingData || _isSavingData)
                return;

            _isSavingData = true;
            try
            {
                _sqliteDataService.SaveDayTimers(Days);
            }
            finally
            {
                _isSavingData = false;
            }
        }

        private void SaveWeekLabels()
        {
            if (_isLoadingData || _isSavingData)
                return;

            _isSavingData = true;
            try
            {
                _sqliteDataService.SaveWeekLabels(
                    WeeklyStats.DayLabel,
                    WeeklyStats.WeekLabel,
                    WeeklyStats.MonthLabel);
            }
            finally
            {
                _isSavingData = false;
            }
        }

        private void ExportToJson()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json",
                FileName = $"target-table-{DateTime.Now:yyyyMMdd-HHmmss}.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            var exportData = new AppExportData
            {
                Tasks = BuildTaskExportData(),
                WorkQueueCells = WorkQueueCells
                    .Select(cell => new PersistedWorkQueueCell
                    {
                        RowIndex = cell.RowIndex,
                        ColumnIndex = cell.ColumnIndex,
                        Value = cell.Value ?? string.Empty
                    })
                    .ToList(),
                DayTimers = Days
                    .Select(day => new PersistedDayTimer
                    {
                        DayIndex = day.DayIndex,
                        ElapsedMilliseconds = day.StoredElapsedMilliseconds,
                        IsRunning = day.IsTimerRunning,
                        RunningStartedUtc = day.RunningStartedUtc
                    })
                    .ToList(),
                Labels = new Dictionary<string, string>
                {
                    ["Day"] = WeeklyStats.DayLabel,
                    ["Week"] = WeeklyStats.WeekLabel,
                    ["Month"] = WeeklyStats.MonthLabel
                }
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            System.IO.File.WriteAllText(dialog.FileName, json);
        }

        private void ImportFromJson()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() != true)
                return;

            var json = System.IO.File.ReadAllText(dialog.FileName);
            var importData = JsonSerializer.Deserialize<AppExportData>(json);
            if (importData == null)
                return;

            _isLoadingData = true;
            try
            {
                ApplyTasks(importData.Tasks ?? new List<PersistedTaskItem>());
                ApplyWorkQueueCells(importData.WorkQueueCells ?? new List<PersistedWorkQueueCell>());
                ApplyDayTimers(importData.DayTimers ?? new List<PersistedDayTimer>());

                WeeklyStats.DayLabel = importData.Labels != null && importData.Labels.TryGetValue("Day", out var day)
                    ? day
                    : string.Empty;
                WeeklyStats.WeekLabel = importData.Labels != null && importData.Labels.TryGetValue("Week", out var week)
                    ? week
                    : string.Empty;
                WeeklyStats.MonthLabel = importData.Labels != null && importData.Labels.TryGetValue("Month", out var month)
                    ? month
                    : string.Empty;
            }
            finally
            {
                _isLoadingData = false;
            }

            RefreshWeeklyStats();
            RefreshDayTimers();
            UndoLastRemovedTaskCommand.RaiseCanExecuteChanged();
            SaveAllState();
        }

        private bool CanUndoLastRemovedTask()
        {
            return GetMostRecentUndoColumn() != null;
        }

        private void UndoLastRemovedTask()
        {
            var targetColumn = GetMostRecentUndoColumn();
            if (targetColumn == null)
                return;

            targetColumn.UndoRemoveRowCommand.Execute(null);
            UndoLastRemovedTaskCommand.RaiseCanExecuteChanged();
        }

        private List<PersistedTaskItem> BuildTaskExportData()
        {
            var rows = new List<PersistedTaskItem>();

            foreach (var day in Days)
            {
                for (int i = 0; i < day.DayTasks.Count; i++)
                {
                    var row = day.DayTasks[i];
                    rows.Add(new PersistedTaskItem
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

            for (int i = 0; i < ToDoColumn.DayTasks.Count; i++)
            {
                var row = ToDoColumn.DayTasks[i];
                rows.Add(new PersistedTaskItem
                {
                    DayIndex = ToDoColumn.DayIndex,
                    DisplayOrder = i,
                    TaskName = row.TaskName ?? string.Empty,
                    Points = row.Points,
                    IsDone = row.IsDone,
                    IsPriority = row.IsPriority,
                    IsToDoColumn = true
                });
            }

            return rows;
        }

        private void ApplyTasks(IEnumerable<PersistedTaskItem> savedRows)
        {
            var rowList = savedRows.ToList();

            foreach (var day in Days)
            {
                day.ClearAllTasks();

                var rowsForDay = rowList
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
                    day.AddTask(task, row.IsDone, row.IsPriority);
                }
            }

            ToDoColumn.ClearAllTasks();
            var toDoRows = rowList
                .Where(x => x.IsToDoColumn)
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            if (toDoRows.Count == 0)
            {
                ToDoColumn.EnsureMinimumRows(1);
            }
            else
            {
                foreach (var row in toDoRows)
                {
                    var task = new TaskModel { Name = row.TaskName, Points = row.Points };
                    ToDoColumn.AddTask(task, row.IsDone, row.IsPriority);
                }
            }
        }

        private void ApplyWorkQueueCells(IEnumerable<PersistedWorkQueueCell> cells)
        {
            foreach (var cell in WorkQueueCells)
                cell.Value = string.Empty;

            foreach (var savedCell in cells)
            {
                var match = WorkQueueCells.FirstOrDefault(cell =>
                    cell.RowIndex == savedCell.RowIndex &&
                    cell.ColumnIndex == savedCell.ColumnIndex);

                if (match != null)
                    match.Value = savedCell.Value ?? string.Empty;
            }
        }

        private void ApplyDayTimers(IEnumerable<PersistedDayTimer> timers)
        {
            var timerMap = timers.ToDictionary(timer => timer.DayIndex, timer => timer.ElapsedMilliseconds);
            var runningMap = timers.ToDictionary(timer => timer.DayIndex, timer => timer);

            foreach (var day in Days)
            {
                var milliseconds = timerMap.TryGetValue(day.DayIndex, out var elapsed) ? elapsed : 0;
                var timerState = runningMap.TryGetValue(day.DayIndex, out var persistedTimer)
                    ? persistedTimer
                    : null;

                day.LoadTimerState(
                    TimeSpan.FromMilliseconds(milliseconds),
                    timerState?.IsRunning ?? false,
                    timerState?.RunningStartedUtc);
            }
        }

        private static DateTime GetStartOfWeek(DateTime date, DayOfWeek startOfWeek)
        {
            int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.AddDays(-diff).Date;
        }

        private DayColumnViewModel? GetMostRecentUndoColumn()
        {
            return Days
                .Append(ToDoColumn)
                .Where(column => column.HasUndoTask && column.LastUndoCreatedUtc.HasValue)
                .OrderByDescending(column => column.LastUndoCreatedUtc)
                .FirstOrDefault();
        }
    }
}
