using System.Collections.Generic;

namespace TimeTableApp.Models
{
    public class AppExportData
    {
        public List<PersistedTaskItem> Tasks { get; set; } = new();

        public List<PersistedWorkQueueCell> WorkQueueCells { get; set; } = new();

        public List<PersistedDayTimer> DayTimers { get; set; } = new();

        public Dictionary<string, string> Labels { get; set; } = new();
    }
}
