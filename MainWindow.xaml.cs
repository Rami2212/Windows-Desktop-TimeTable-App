using System.Windows;
using TimeTableApp.ViewModels;

namespace TimeTableApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        /// <summary>
        /// Triggered when any period-label TextBox loses focus.
        /// Tells WeeklyStatsViewModel to persist the current label values.
        /// </summary>
        private void OnLabelLostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.WeeklyStats.NotifyLabelsChanged();
        }
    }
}