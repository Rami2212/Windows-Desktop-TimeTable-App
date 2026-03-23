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

        public string DayName
        {
            get => _dayName;
            set
            {
                _dayName = value;
                OnPropertyChanged(nameof(DayName));
            }
        }

        public ObservableCollection<DayTaskStatus> DayTasks { get; } = new ObservableCollection<DayTaskStatus>();

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
            DayTasks.CollectionChanged += OnDayTasksCollectionChanged;
        }

        public void AddTask(TaskModel task)
        {
            var status = new DayTaskStatus(task);
            status.PropertyChanged += OnTaskStatusChanged;
            DayTasks.Add(status);
            RefreshTotals();
        }

        public void RemoveTask(TaskModel task)
        {
            var existing = DayTasks.FirstOrDefault(x => x.Task == task);
            if (existing != null)
            {
                existing.PropertyChanged -= OnTaskStatusChanged;
                DayTasks.Remove(existing);
                RefreshTotals();
            }
        }

        private void OnDayTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshTotals();
        }

        private void OnTaskStatusChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DayTaskStatus.IsDone) ||
                e.PropertyName == nameof(DayTaskStatus.Points))
            {
                RefreshTotals();
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