using System;

namespace TimeTableApp.Models
{
    public class PersistedDayTimer
    {
        public int Id { get; set; }

        public int DayIndex { get; set; }

        public long ElapsedMilliseconds { get; set; }

        public bool IsRunning { get; set; }

        public DateTime? RunningStartedUtc { get; set; }

        public DateTime? TimerDate { get; set; }
    }
}
