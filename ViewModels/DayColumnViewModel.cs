using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;
using TimeTableApp.Models;

namespace TimeTableApp.ViewModels
{
    using TaskModel = TimeTableApp.Models.Task;

    public class DayColumnViewModel : BaseViewModel
    {
        private const int MinRequiredPoints = 10;

        private readonly DispatcherTimer _workTimer;
        private string _dayName = string.Empty;
        private DayTaskStatus? _selectedDayTask;
        private TimeSpan _elapsedWorkTime;
        private bool _isTimerRunning;
        private bool _isEditingWorkTimer;
        private string _editableWorkTimerText = "00:00:00.00";

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

        public int DayIndex { get; }

        public bool IsToDoColumn { get; }

        public DateTime? ColumnDate { get; }

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

        public TimeSpan ElapsedWorkTime
        {
            get => _elapsedWorkTime;
            private set
            {
                if (_elapsedWorkTime != value)
                {
                    _elapsedWorkTime = value;
                    _editableWorkTimerText = WorkTimerDisplay;
                    OnPropertyChanged(nameof(ElapsedWorkTime));
                    OnPropertyChanged(nameof(WorkTimerDisplay));
                    OnPropertyChanged(nameof(EditableWorkTimerText));
                }
            }
        }

        public string WorkTimerDisplay => ElapsedWorkTime.ToString(@"hh\:mm\:ss\.ff");

        public string EditableWorkTimerText
        {
            get => _editableWorkTimerText;
            set
            {
                if (_editableWorkTimerText != value)
                {
                    _editableWorkTimerText = value;
                    OnPropertyChanged(nameof(EditableWorkTimerText));
                }
            }
        }

        public bool IsTimerRunning
        {
            get => _isTimerRunning;
            private set
            {
                if (_isTimerRunning != value)
                {
                    _isTimerRunning = value;
                    OnPropertyChanged(nameof(IsTimerRunning));
                }
            }
        }

        public bool IsEditingWorkTimer
        {
            get => _isEditingWorkTimer;
            private set
            {
                if (_isEditingWorkTimer != value)
                {
                    _isEditingWorkTimer = value;
                    OnPropertyChanged(nameof(IsEditingWorkTimer));
                    OnPropertyChanged(nameof(IsWorkTimerReadOnly));
                }
            }
        }

        public bool IsWorkTimerReadOnly => !IsEditingWorkTimer;

        public bool ShowWorkTimer => !IsToDoColumn;

        public bool IsTodayColumn =>
            !IsToDoColumn &&
            ColumnDate.HasValue &&
            ColumnDate.Value.Date == DateTime.Today;

        public bool IsTimerFrozen =>
            ShowWorkTimer &&
            ColumnDate.HasValue &&
            DateTime.Today > ColumnDate.Value.Date;

        public bool CanStartOrPauseTimer =>
            ShowWorkTimer &&
            ColumnDate.HasValue &&
            DateTime.Today == ColumnDate.Value.Date &&
            !IsTimerFrozen;

        public bool CanEditFrozenTimer => IsTimerFrozen;

        public RelayCommand AddRowCommand { get; }
        public RelayCommand RemoveRowCommand { get; }
        public RelayCommand TogglePriorityCommand { get; }
        public RelayCommand ToggleTimerCommand { get; }
        public RelayCommand ToggleTimerEditCommand { get; }

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

        public bool IsBelowMinPoints =>
            !IsToDoColumn && TotalPossiblePoints < MinRequiredPoints;

        public bool IsBelowProgressThreshold =>
            !IsToDoColumn && ProgressValue < 80.0 && TotalPossiblePoints > 0;

        public event Action? DataChanged;
        public event Action? TimerSaved;

        public DayColumnViewModel(string dayName, int dayIndex, DateTime? columnDate = null, bool isToDoColumn = false)
        {
            DayName = dayName;
            DayIndex = dayIndex;
            ColumnDate = columnDate;
            IsToDoColumn = isToDoColumn;

            AddRowCommand = new RelayCommand(AddBlankTaskRow);
            RemoveRowCommand = new RelayCommand(RemoveRowFromParameter);
            TogglePriorityCommand = new RelayCommand(TogglePriorityFromParameter);
            ToggleTimerCommand = new RelayCommand(ToggleWorkTimer, () => CanStartOrPauseTimer);
            ToggleTimerEditCommand = new RelayCommand(ToggleTimerEdit, () => CanEditFrozenTimer);

            _workTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _workTimer.Tick += OnWorkTimerTick;

            DayTasks.CollectionChanged += OnDayTasksCollectionChanged;
            RefreshTimerAvailability();
        }

        public void AddTask(TaskModel task, bool isDone = false, bool isPriority = false)
        {
            var status = new DayTaskStatus(task) { IsDone = isDone, IsPriority = isPriority };
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

        public void LoadTimerState(TimeSpan elapsedTime)
        {
            ElapsedWorkTime = elapsedTime;
            EditableWorkTimerText = WorkTimerDisplay;
            IsEditingWorkTimer = false;
            PauseInternal();
            RefreshTimerAvailability();
        }

        public void RefreshTimerAvailability()
        {
            if (IsTimerFrozen)
            {
                _workTimer.Stop();
                IsTimerRunning = false;
            }

            OnPropertyChanged(nameof(ShowWorkTimer));
            OnPropertyChanged(nameof(IsTimerFrozen));
            OnPropertyChanged(nameof(CanStartOrPauseTimer));
            OnPropertyChanged(nameof(CanEditFrozenTimer));
            ToggleTimerCommand.RaiseCanExecuteChanged();
            ToggleTimerEditCommand.RaiseCanExecuteChanged();
        }

        public void NotifyMoved() => NotifyDataChanged();

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

        private void ToggleWorkTimer()
        {
            if (!CanStartOrPauseTimer)
                return;

            if (IsTimerRunning)
            {
                PauseInternal();
                NotifyTimerSaved();
            }
            else
            {
                IsEditingWorkTimer = false;
                _workTimer.Start();
                IsTimerRunning = true;
                RefreshTimerAvailability();
            }
        }

        private void ToggleTimerEdit()
        {
            if (!CanEditFrozenTimer)
                return;

            if (!IsEditingWorkTimer)
            {
                EditableWorkTimerText = WorkTimerDisplay;
                IsEditingWorkTimer = true;
                return;
            }

            if (TimeSpan.TryParse(EditableWorkTimerText, out var parsed))
            {
                ElapsedWorkTime = parsed;
                IsEditingWorkTimer = false;
                NotifyTimerSaved();
            }
        }

        private void PauseInternal()
        {
            _workTimer.Stop();
            IsTimerRunning = false;
            OnPropertyChanged(nameof(IsTimerFrozen));
            OnPropertyChanged(nameof(CanStartOrPauseTimer));
            ToggleTimerCommand.RaiseCanExecuteChanged();
        }

        private void OnWorkTimerTick(object? sender, EventArgs e)
        {
            if (IsTimerFrozen || !CanStartOrPauseTimer)
            {
                PauseInternal();
                NotifyTimerSaved();
                return;
            }

            ElapsedWorkTime = ElapsedWorkTime.Add(_workTimer.Interval);
        }

        private void RemoveRowFromParameter(object? parameter)
        {
            if (parameter is DayTaskStatus row)
                RemoveTask(row);
        }

        private void TogglePriorityFromParameter(object? parameter)
        {
            if (parameter is not DayTaskStatus row)
                return;

            row.IsPriority = !row.IsPriority;
            NotifyDataChanged();
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
                e.PropertyName == nameof(DayTaskStatus.TaskName) ||
                e.PropertyName == nameof(DayTaskStatus.IsPriority))
            {
                RefreshTotals();
                NotifyDataChanged();
            }
        }

        private void NotifyDataChanged() => DataChanged?.Invoke();

        private void NotifyTimerSaved() => TimerSaved?.Invoke();
    }
}
