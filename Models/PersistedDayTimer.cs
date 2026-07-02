namespace TimeTableApp.Models
{
    public class PersistedDayTimer
    {
        public int Id { get; set; }

        public int DayIndex { get; set; }

        public long ElapsedMilliseconds { get; set; }
    }
}
