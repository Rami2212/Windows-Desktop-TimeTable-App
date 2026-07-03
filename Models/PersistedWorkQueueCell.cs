namespace TimeTableApp.Models
{
    public class PersistedWorkQueueCell
    {
        public int Id { get; set; }

        public int RowIndex { get; set; }

        public int ColumnIndex { get; set; }

        public string Value { get; set; } = string.Empty;
    }
}
