namespace TimeTableApp.Models
{
    public class PersistedTaskItem
    {
        public int Id { get; set; }

        // 0 = Monday, 1 = Tuesday, ... 6 = Sunday, 7 = To Do column
        public int DayIndex { get; set; }

        // Row position inside each day column
        public int DisplayOrder { get; set; }

        public string TaskName { get; set; } = string.Empty;

        public int Points { get; set; }

        public bool IsDone { get; set; }

        // True if this belongs to the standalone "To Do" column
        public bool IsToDoColumn { get; set; }
    }
}