using System;
using System.Collections.ObjectModel;

namespace TimeTableApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ObservableCollection<DayColumnViewModel> Days { get; } = new ObservableCollection<DayColumnViewModel>();

        public MainViewModel()
        {
            var startOfWeek = GetStartOfWeek(DateTime.Today, DayOfWeek.Monday);

            for (int i = 0; i < 7; i++)
            {
                var currentDate = startOfWeek.AddDays(i);
                var dayLabel = $"{currentDate:dddd} - {currentDate:dd}";

                var dayViewModel = new DayColumnViewModel(dayLabel);
                dayViewModel.EnsureMinimumRows(10);

                Days.Add(dayViewModel);
            }
        }

        private static DateTime GetStartOfWeek(DateTime date, DayOfWeek startOfWeek)
        {
            int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.AddDays(-diff).Date;
        }
    }
}