using System;

namespace TimeTableApp.ViewModels
{
    public class WeeklyStatsViewModel : BaseViewModel
    {
        private const int WeeklyMinPoints = 70;

        private string _dayLabel = string.Empty;
        private string _weekLabel = string.Empty;
        private string _monthLabel = string.Empty;

        public string DayLabel
        {
            get => _dayLabel;
            set { if (_dayLabel != value) { _dayLabel = value; OnPropertyChanged(nameof(DayLabel)); } }
        }

        public string WeekLabel
        {
            get => _weekLabel;
            set { if (_weekLabel != value) { _weekLabel = value; OnPropertyChanged(nameof(WeekLabel)); } }
        }

        public string MonthLabel
        {
            get => _monthLabel;
            set { if (_monthLabel != value) { _monthLabel = value; OnPropertyChanged(nameof(MonthLabel)); } }
        }

        // ── Aggregated stats (set by MainViewModel) ──────────────────────────

        private int _weeklyTotalPoints;
        public int WeeklyTotalPoints
        {
            get => _weeklyTotalPoints;
            set
            {
                if (_weeklyTotalPoints != value)
                {
                    _weeklyTotalPoints = value;
                    OnPropertyChanged(nameof(WeeklyTotalPoints));
                    OnPropertyChanged(nameof(IsBelowWeeklyMinPoints));
                    OnPropertyChanged(nameof(WeeklyMinLabel));
                }
            }
        }

        private int _weeklyCompletedPoints;
        public int WeeklyCompletedPoints
        {
            get => _weeklyCompletedPoints;
            set
            {
                if (_weeklyCompletedPoints != value)
                {
                    _weeklyCompletedPoints = value;
                    OnPropertyChanged(nameof(WeeklyCompletedPoints));
                    OnPropertyChanged(nameof(WeeklyProgressValue));
                    OnPropertyChanged(nameof(WeeklyProgressText));
                }
            }
        }

        public double WeeklyProgressValue =>
            WeeklyTotalPoints == 0
                ? 0
                : (double)WeeklyCompletedPoints / WeeklyTotalPoints * 100.0;

        public string WeeklyProgressText => $"{WeeklyProgressValue:F0}%";

        /// <summary>True when total possible points for the week are below 70.</summary>
        public bool IsBelowWeeklyMinPoints => WeeklyTotalPoints < WeeklyMinPoints;

        /// <summary>Label shown next to the total, e.g. "⚠ min 70" when below threshold.</summary>
        public string WeeklyMinLabel => IsBelowWeeklyMinPoints
            ? $"⚠ min {WeeklyMinPoints}"
            : string.Empty;

        public event Action? LabelsChanged;

        public void NotifyLabelsChanged() => LabelsChanged?.Invoke();
    }
}