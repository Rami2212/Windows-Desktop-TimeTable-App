using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using TimeTableApp.Models;

namespace TimeTableApp.ViewModels
{
    using TaskModel = TimeTableApp.Models.Task;

    public class DayColumnViewModel : BaseViewModel
    {
        private const int MinRequiredPoints = 10;

        private string _dayName = string.Empty;
        private DayTaskStatus? _selectedDayTask;

        public string DayName
        {
            get => _dayName;
            set
            {
                if (_dayName != value)
                {
                    _dayName = value;
                    OnPropertyChanged(nameof(DayName));
                }
            }
        }

        // 0=Mon … 6=Sun, 7=To Do
        public int DayIndex { get; }

        /// <summary>True for the standalone "To Do" column — excluded from stats.</summary>
        public bool IsToDoColumn { get; }

        public ObservableCollection<DayTaskStatus> DayTasks { get; } = new ObservableCollection<DayTaskStatus>();

        public DayTaskStatus? SelectedDayTask
        {
            get => _selectedDayTask;
            set
            {
                if (_selectedDayTask != value)
                {
                    _selectedDayTask = value;
                    OnPropertyChanged(nameof(SelectedDayTask));
                }
            }
        }

        public RelayCommand AddRowCommand { get; }
        public RelayCommand RemoveRowCommand { get; }

        public int TotalCompletedPoints => DayTasks.Where(t => t.IsDone).Sum(t => t.Points);
        public int TotalPossiblePoints => DayTasks.Sum(t => t.Points);

        public double ProgressValue
        {
            get
            {
                if (TotalPossiblePoints == 0) return 0;
                return (double)TotalCompletedPoints / TotalPossiblePoints * 100.0;
            }
        }

        /// <summary>True when total possible points are below the per-day minimum (10).</summary>
        public bool IsBelowMinPoints =>
            !IsToDoColumn && TotalPossiblePoints < MinRequiredPoints;

        /// <summary>True when completion progress is below 80 %.</summary>
        public bool IsBelowProgressThreshold =>
            !IsToDoColumn && ProgressValue < 80.0 && TotalPossiblePoints > 0;

        public event Action? DataChanged;

        public DayColumnViewModel(string dayName, int dayIndex, bool isToDoColumn = false)
        {
            DayName = dayName;
            DayIndex = dayIndex;
            IsToDoColumn = isToDoColumn;

            AddRowCommand = new RelayCommand(AddBlankTaskRow);
            RemoveRowCommand = new RelayCommand(RemoveRowFromParameter);

            DayTasks.CollectionChanged += OnDayTasksCollectionChanged;
        }

        // ── Task management ──────────────────────────────────────────────────

        public void AddTask(TaskModel task, bool isDone = false)
        {
            var status = new DayTaskStatus(task) { IsDone = isDone };
            status.PropertyChanged += OnTaskStatusChanged;
            DayTasks.Add(status);
            RefreshTotals();
        }

        public void AddBlankTaskRow()
        {
            var task = new TaskModel { Name = string.Empty, Points = 0 };
            AddTask(task);
        }

        public void RemoveTask(DayTaskStatus? dayTaskStatus)
        {
            if (dayTaskStatus == null) return;

            dayTaskStatus.PropertyChanged -= OnTaskStatusChanged;
            DayTasks.Remove(dayTaskStatus);

            if (SelectedDayTask == dayTaskStatus)
                SelectedDayTask = null;

            RefreshTotals();
            NotifyDataChanged();
        }

        public void ClearAllTasks()
        {
            foreach (var task in DayTasks)
                task.PropertyChanged -= OnTaskStatusChanged;

            DayTasks.Clear();
            SelectedDayTask = null;
            RefreshTotals();
        }

        /// <summary>
        /// Called by DragDropBehavior after it moves an item in DayTasks.
        /// Triggers persistence without double-firing CollectionChanged logic.
        /// </summary>
        public void NotifyMoved() => NotifyDataChanged();

        // ── Privates ─────────────────────────────────────────────────────────

        private void RemoveRowFromParameter(object? parameter)
        {
            if (parameter is DayTaskStatus row)
                RemoveTask(row);
        }

        private void OnDayTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshTotals();
            NotifyDataChanged();
        }

        private void OnTaskStatusChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DayTaskStatus.IsDone) ||
                e.PropertyName == nameof(DayTaskStatus.Points) ||
                e.PropertyName == nameof(DayTaskStatus.TaskName))
            {
                RefreshTotals();
                NotifyDataChanged();
            }
        }

        public void EnsureMinimumRows(int minimumRowCount)
        {
            while (DayTasks.Count < minimumRowCount)
                AddBlankTaskRow();
        }

        public void RefreshTotals()
        {
            OnPropertyChanged(nameof(TotalCompletedPoints));
            OnPropertyChanged(nameof(TotalPossiblePoints));
            OnPropertyChanged(nameof(ProgressValue));
            OnPropertyChanged(nameof(IsBelowMinPoints));
            OnPropertyChanged(nameof(IsBelowProgressThreshold));
        }

        private void NotifyDataChanged() => DataChanged?.Invoke();
    }
}