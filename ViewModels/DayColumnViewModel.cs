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
                if (TotalPossiblePoints == 0)
                    return 0;

                return (double)TotalCompletedPoints / TotalPossiblePoints * 100.0;
            }
        }

        public DayColumnViewModel(string dayName)
        {
            DayName = dayName;

            AddRowCommand = new RelayCommand(AddBlankTaskRow);
            RemoveRowCommand = new RelayCommand(RemoveRowFromParameter);

            DayTasks.CollectionChanged += OnDayTasksCollectionChanged;
        }

        public void AddTask(TaskModel task)
        {
            var status = new DayTaskStatus(task);
            status.PropertyChanged += OnTaskStatusChanged;
            DayTasks.Add(status);
            RefreshTotals();
        }

        public void AddBlankTaskRow()
        {
            var task = new TaskModel
            {
                Name = string.Empty,
                Points = 0
            };

            AddTask(task);
        }

        public void RemoveTask(DayTaskStatus? dayTaskStatus)
        {
            if (dayTaskStatus == null)
                return;

            dayTaskStatus.PropertyChanged -= OnTaskStatusChanged;
            DayTasks.Remove(dayTaskStatus);

            if (SelectedDayTask == dayTaskStatus)
            {
                SelectedDayTask = null;
            }

            RefreshTotals();
        }

        private void RemoveRowFromParameter(object? parameter)
        {
            if (parameter is DayTaskStatus row)
            {
                RemoveTask(row);
            }
        }

        private void OnDayTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshTotals();
        }

        private void OnTaskStatusChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DayTaskStatus.IsDone) ||
                e.PropertyName == nameof(DayTaskStatus.Points) ||
                e.PropertyName == nameof(DayTaskStatus.TaskName))
            {
                RefreshTotals();
            }
        }

        public void EnsureMinimumRows(int minimumRowCount)
        {
            while (DayTasks.Count < minimumRowCount)
            {
                AddBlankTaskRow();
            }
        }

        public void RefreshTotals()
        {
            OnPropertyChanged(nameof(TotalCompletedPoints));
            OnPropertyChanged(nameof(TotalPossiblePoints));
            OnPropertyChanged(nameof(ProgressValue));
        }
    }
}