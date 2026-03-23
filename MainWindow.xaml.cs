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
    }
}