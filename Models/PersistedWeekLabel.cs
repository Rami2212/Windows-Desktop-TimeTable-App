namespace TimeTableApp.Models
{
    public class PersistedWeekLabel
    {
        public int Id { get; set; }

        // "Day", "Week", or "Month"
        public string LabelKey { get; set; } = string.Empty;

        public string LabelValue { get; set; } = string.Empty;
    }
}