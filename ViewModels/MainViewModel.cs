using System.Collections.ObjectModel;
using TimeTableApp.Models;

namespace TimeTableApp.ViewModels
{
    using TaskModel = TimeTableApp.Models.Task;

    public class MainViewModel : BaseViewModel
    {
        private string _taskNameInput = string.Empty;
        private string _taskPointsInput = string.Empty;
        private TaskModel? _selectedTask;

        public ObservableCollection<TaskModel> Tasks { get; } = new ObservableCollection<TaskModel>();
        public ObservableCollection<DayColumnViewModel> Days { get; } = new ObservableCollection<DayColumnViewModel>();

        public string TaskNameInput
        {
            get => _taskNameInput;
            set
            {
                _taskNameInput = value;
                OnPropertyChanged(nameof(TaskNameInput));
                RaiseCommandStates();
            }
        }

        public string TaskPointsInput
        {
            get => _taskPointsInput;
            set
            {
                _taskPointsInput = value;
                OnPropertyChanged(nameof(TaskPointsInput));
                RaiseCommandStates();
            }
        }

        public TaskModel? SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                OnPropertyChanged(nameof(SelectedTask));

                if (_selectedTask != null)
                {
                    TaskNameInput = _selectedTask.Name;
                    TaskPointsInput = _selectedTask.Points.ToString();
                }

                RaiseCommandStates();
            }
        }

        public RelayCommand AddTaskCommand { get; }
        public RelayCommand UpdateTaskCommand { get; }
        public RelayCommand RemoveTaskCommand { get; }
        public RelayCommand ClearInputsCommand { get; }

        public MainViewModel()
        {
            Days.Add(new DayColumnViewModel("Monday"));
            Days.Add(new DayColumnViewModel("Tuesday"));
            Days.Add(new DayColumnViewModel("Wednesday"));
            Days.Add(new DayColumnViewModel("Thursday"));
            Days.Add(new DayColumnViewModel("Friday"));
            Days.Add(new DayColumnViewModel("Saturday"));
            Days.Add(new DayColumnViewModel("Sunday"));

            AddTaskCommand = new RelayCommand(AddTask, CanAddTask);
            UpdateTaskCommand = new RelayCommand(UpdateTask, CanUpdateTask);
            RemoveTaskCommand = new RelayCommand(RemoveTask, CanRemoveTask);
            ClearInputsCommand = new RelayCommand(ClearInputs);

            AddSampleTask("Study", 3);
            AddSampleTask("Workout", 2);
            AddSampleTask("Read Book", 1);
        }

        private void AddSampleTask(string name, int points)
        {
            var task = new TaskModel { Name = name, Points = points };
            Tasks.Add(task);

            foreach (var day in Days)
            {
                day.AddTask(task);
            }
        }

        private bool CanAddTask()
        {
            return !string.IsNullOrWhiteSpace(TaskNameInput) &&
                   int.TryParse(TaskPointsInput, out int points) &&
                   points >= 0;
        }

        private void AddTask()
        {
            if (!int.TryParse(TaskPointsInput, out int points)) return;

            var newTask = new TaskModel
            {
                Name = TaskNameInput.Trim(),
                Points = points
            };

            Tasks.Add(newTask);

            foreach (var day in Days)
            {
                day.AddTask(newTask);
            }

            ClearInputs();
        }

        private bool CanUpdateTask()
        {
            return SelectedTask != null &&
                   !string.IsNullOrWhiteSpace(TaskNameInput) &&
                   int.TryParse(TaskPointsInput, out int points);
        }

        private void UpdateTask()
        {
            if (SelectedTask == null) return;

            SelectedTask.Name = TaskNameInput.Trim();
            SelectedTask.Points = int.Parse(TaskPointsInput);

            foreach (var day in Days)
            {
                day.RefreshTotals();
            }
        }

        private bool CanRemoveTask() => SelectedTask != null;

        private void RemoveTask()
        {
            if (SelectedTask == null) return;

            var taskToRemove = SelectedTask;

            foreach (var day in Days)
            {
                day.RemoveTask(taskToRemove);
            }

            Tasks.Remove(taskToRemove);
            ClearInputs();
        }

        private void ClearInputs()
        {
            _selectedTask = null;
            _taskNameInput = string.Empty;
            _taskPointsInput = string.Empty;

            OnPropertyChanged(nameof(SelectedTask));
            OnPropertyChanged(nameof(TaskNameInput));
            OnPropertyChanged(nameof(TaskPointsInput));

            RaiseCommandStates();
        }

        private void RaiseCommandStates()
        {
            AddTaskCommand.RaiseCanExecuteChanged();
            UpdateTaskCommand.RaiseCanExecuteChanged();
            RemoveTaskCommand.RaiseCanExecuteChanged();
        }
    }
}