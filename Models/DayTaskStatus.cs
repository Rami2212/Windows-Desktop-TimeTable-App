using System.ComponentModel;
using TaskModel = TimeTableApp.Models.Task;

namespace TimeTableApp.Models
{
    public class DayTaskStatus : INotifyPropertyChanged
    {
        private bool _isDone;

        public TaskModel Task { get; }

        public string TaskName
        {
            get => Task.Name;
            set
            {
                if (Task.Name != value)
                {
                    Task.Name = value;
                    OnPropertyChanged(nameof(TaskName));
                }
            }
        }

        public int Points
        {
            get => Task.Points;
            set
            {
                if (Task.Points != value)
                {
                    Task.Points = value;
                    OnPropertyChanged(nameof(Points));
                }
            }
        }

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

            Task.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Task.Name))
                {
                    OnPropertyChanged(nameof(TaskName));
                }

                if (e.PropertyName == nameof(Task.Points))
                {
                    OnPropertyChanged(nameof(Points));
                }
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}