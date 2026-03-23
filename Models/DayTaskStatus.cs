using System.ComponentModel;
using TaskModel = TimeTableApp.Models.Task;

namespace TimeTableApp.Models
{
    public class DayTaskStatus : INotifyPropertyChanged
    {
        private bool _isDone;

        public TaskModel Task { get; }

        public string TaskName => Task.Name;
        public int Points => Task.Points;

        public bool IsDone
        {
            get => _isDone;
            set
            {
                if (_isDone != value)
                {
                    _isDone = value;
                    OnPropertyChanged(nameof(IsDone));
                }
            }
        }

        public DayTaskStatus(TaskModel task)
        {
            Task = task;

            Task.PropertyChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(TaskName));
                OnPropertyChanged(nameof(Points));
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}