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
        private readonly DispatcherTimer _undoDismissTimer;
        private string _dayName = string.Empty;
        private DayTaskStatus? _selectedDayTask;
        private TimeSpan _elapsedWorkTime;
        private long _storedElapsedMilliseconds;
        private bool _isTimerRunning;
        private bool _isEditingWorkTimer;
        private string _editableWorkTimerText = "00:00:00.00";
        private DateTime? _runningStartedUtc;
        private DayTaskStatus? _lastRemovedTask;
        private int _lastRemovedTaskIndex = -1;
        private string _undoMessage = string.Empty;
        private DateTime? _lastUndoCreatedUtc;

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

        public long StoredElapsedMilliseconds => _storedElapsedMilliseconds;

        public DateTime? RunningStartedUtc => _runningStartedUtc;

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

        public bool HasUndoTask => _lastRemovedTask != null;

        public string UndoMessage
        {
            get => _undoMessage;
            private set
            {
                if (_undoMessage != value)
                {
                    _undoMessage = value;
                    OnPropertyChanged(nameof(UndoMessage));
                }
            }
        }

        public DateTime? LastUndoCreatedUtc
        {
            get => _lastUndoCreatedUtc;
            private set
            {
                if (_lastUndoCreatedUtc != value)
                {
                    _lastUndoCreatedUtc = value;
                    OnPropertyChanged(nameof(LastUndoCreatedUtc));
                }
            }
        }

        public RelayCommand AddRowCommand { get; }
        public RelayCommand RemoveRowCommand { get; }
        public RelayCommand UndoRemoveRowCommand { get; }
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
        public event Action? UndoStateChanged;

        public DayColumnViewModel(string dayName, int dayIndex, DateTime? columnDate = null, bool isToDoColumn = false)
        {
            DayName = dayName;
            DayIndex = dayIndex;
            ColumnDate = columnDate;
            IsToDoColumn = isToDoColumn;

            AddRowCommand = new RelayCommand(AddBlankTaskRow);
            RemoveRowCommand = new RelayCommand(RemoveRowFromParameter);
            UndoRemoveRowCommand = new RelayCommand(UndoLastRemovedTask, () => HasUndoTask);
            TogglePriorityCommand = new RelayCommand(TogglePriorityFromParameter);
            ToggleTimerCommand = new RelayCommand(ToggleWorkTimer, () => CanStartOrPauseTimer);
            ToggleTimerEditCommand = new RelayCommand(ToggleTimerEdit, () => CanEditFrozenTimer);

            _workTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _workTimer.Tick += OnWorkTimerTick;

            _undoDismissTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(6)
            };
            _undoDismissTimer.Tick += (_, _) => ClearUndoState();

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

            _lastRemovedTaskIndex = DayTasks.IndexOf(dayTaskStatus);
            _lastRemovedTask = dayTaskStatus;
            LastUndoCreatedUtc = DateTime.UtcNow;
            UndoMessage = string.IsNullOrWhiteSpace(dayTaskStatus.TaskName)
                ? "Task removed"
                : $"Removed: {dayTaskStatus.TaskName}";
            OnPropertyChanged(nameof(HasUndoTask));
            UndoRemoveRowCommand.RaiseCanExecuteChanged();
            NotifyUndoStateChanged();
            _undoDismissTimer.Stop();
            _undoDismissTimer.Start();

            dayTaskStatus.PropertyChanged -= OnTaskStatusChanged;
            DayTasks.Remove(dayTaskStatus);

            if (SelectedDayTask == dayTaskStatus)
                SelectedDayTask = null;

            RefreshTotals();
            NotifyDataChanged();
        }

        public void ClearAllTasks()
        {
            ClearUndoState();

            foreach (var task in DayTasks)
                task.PropertyChanged -= OnTaskStatusChanged;

            DayTasks.Clear();
            SelectedDayTask = null;
            RefreshTotals();
        }

        public void LoadTimerState(TimeSpan elapsedTime, bool isRunning = false, DateTime? runningStartedUtc = null)
        {
            _storedElapsedMilliseconds = (long)elapsedTime.TotalMilliseconds;
            _runningStartedUtc = isRunning ? runningStartedUtc : null;
            ElapsedWorkTime = CalculateCurrentElapsed(DateTime.UtcNow);
            EditableWorkTimerText = WorkTimerDisplay;
            IsEditingWorkTimer = false;
            if (isRunning && CanStartOrPauseTimer)
            {
                SetRunningState(true);
            }
            else
            {
                if (isRunning)
                    FinalizeRunningTimer(ElapsedWorkTime);
                else
                    PauseInternal();
            }
            RefreshTimerAvailability();
        }

        public void RefreshTimerAvailability()
        {
            if (_runningStartedUtc.HasValue)
            {
                var nowUtc = DateTime.UtcNow;
                var currentElapsed = CalculateCurrentElapsed(nowUtc);

                if (IsTimerFrozen || !CanStartOrPauseTimer)
                {
                    FinalizeRunningTimer(currentElapsed);
                    NotifyTimerSaved();
                }
                else if (IsTimerRunning)
                {
                    ElapsedWorkTime = currentElapsed;
                }
            }

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
                FinalizeRunningTimer(CalculateCurrentElapsed(DateTime.UtcNow));
                NotifyTimerSaved();
            }
            else
            {
                IsEditingWorkTimer = false;
                _runningStartedUtc = DateTime.UtcNow;
                ElapsedWorkTime = CalculateCurrentElapsed(_runningStartedUtc.Value);
                _workTimer.Start();
                SetRunningState(true);
                RefreshTimerAvailability();
                NotifyTimerSaved();
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
                _storedElapsedMilliseconds = (long)parsed.TotalMilliseconds;
                _runningStartedUtc = null;
                ElapsedWorkTime = parsed;
                IsEditingWorkTimer = false;
                NotifyTimerSaved();
            }
        }

        private void PauseInternal()
        {
            _workTimer.Stop();
            SetRunningState(false);
            OnPropertyChanged(nameof(IsTimerFrozen));
            OnPropertyChanged(nameof(CanStartOrPauseTimer));
            ToggleTimerCommand.RaiseCanExecuteChanged();
        }

        private void OnWorkTimerTick(object? sender, EventArgs e)
        {
            if (IsTimerFrozen || !CanStartOrPauseTimer)
            {
                FinalizeRunningTimer(CalculateCurrentElapsed(DateTime.UtcNow));
                NotifyTimerSaved();
                return;
            }

            ElapsedWorkTime = CalculateCurrentElapsed(DateTime.UtcNow);
        }

        private void RemoveRowFromParameter(object? parameter)
        {
            if (parameter is DayTaskStatus row)
                RemoveTask(row);
        }

        private void UndoLastRemovedTask()
        {
            if (_lastRemovedTask == null)
                return;

            var insertIndex = _lastRemovedTaskIndex;
            if (insertIndex < 0 || insertIndex > DayTasks.Count)
                insertIndex = DayTasks.Count;

            _lastRemovedTask.PropertyChanged += OnTaskStatusChanged;
            DayTasks.Insert(insertIndex, _lastRemovedTask);
            RefreshTotals();
            NotifyDataChanged();
            ClearUndoState();
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

        private void SetRunningState(bool isRunning)
        {
            if (isRunning)
            {
                _workTimer.Start();
            }
            else
            {
                _workTimer.Stop();
            }

            IsTimerRunning = isRunning;
        }

        private void FinalizeRunningTimer(TimeSpan finalElapsed)
        {
            _storedElapsedMilliseconds = (long)finalElapsed.TotalMilliseconds;
            _runningStartedUtc = null;
            ElapsedWorkTime = finalElapsed;
            PauseInternal();
        }

        private TimeSpan CalculateCurrentElapsed(DateTime utcNow)
        {
            var elapsed = TimeSpan.FromMilliseconds(_storedElapsedMilliseconds);

            if (!_runningStartedUtc.HasValue)
                return elapsed;

            var effectiveUtcNow = utcNow;
            var dayEndUtc = GetDayEndUtc();
            if (dayEndUtc.HasValue && effectiveUtcNow > dayEndUtc.Value)
                effectiveUtcNow = dayEndUtc.Value;

            if (effectiveUtcNow <= _runningStartedUtc.Value)
                return elapsed;

            return elapsed.Add(effectiveUtcNow - _runningStartedUtc.Value);
        }

        private DateTime? GetDayEndUtc()
        {
            if (!ColumnDate.HasValue)
                return null;

            var localDayEnd = ColumnDate.Value.Date.AddDays(1);
            return localDayEnd.ToUniversalTime();
        }

        private void ClearUndoState()
        {
            _undoDismissTimer.Stop();
            _lastRemovedTask = null;
            _lastRemovedTaskIndex = -1;
            LastUndoCreatedUtc = null;
            UndoMessage = string.Empty;
            OnPropertyChanged(nameof(HasUndoTask));
            UndoRemoveRowCommand.RaiseCanExecuteChanged();
            NotifyUndoStateChanged();
        }

        private void NotifyUndoStateChanged() => UndoStateChanged?.Invoke();
    }
}
