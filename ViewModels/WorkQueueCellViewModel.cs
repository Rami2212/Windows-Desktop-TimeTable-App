namespace TimeTableApp.ViewModels
{
    public class WorkQueueCellViewModel : BaseViewModel
    {
        private string _value = string.Empty;

        public int RowIndex { get; }

        public int ColumnIndex { get; }

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public WorkQueueCellViewModel(int rowIndex, int columnIndex)
        {
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }
    }
}
