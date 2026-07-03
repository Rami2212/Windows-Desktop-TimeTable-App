using System.Reflection;
using System.Windows.Input;
using System.Windows;
using TimeTableApp.ViewModels;

namespace TimeTableApp
{
    public partial class MainWindow : Window
    {
        public string AppVersion =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(2) ?? "3.0";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            Loaded += OnWindowLoaded;
            StateChanged += OnWindowStateChanged;
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

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleWindowState();
                return;
            }

            DragMove();
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleWindowState()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ApplyWindowBounds();
        }

        private void OnWindowStateChanged(object? sender, EventArgs e)
        {
            ApplyWindowBounds();
        }

        private void ApplyWindowBounds()
        {
            if (WindowState == WindowState.Maximized)
            {
                MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                return;
            }

            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;
        }
    }
}
