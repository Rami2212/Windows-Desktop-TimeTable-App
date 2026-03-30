using System;
using System.Globalization;
using System.Windows.Data;

namespace TimeTableApp.Converters
{
    /// <summary>Returns true when a double value is below 80.</summary>
    [ValueConversion(typeof(double), typeof(bool))]
    public class BelowEightyConverter : IValueConverter
    {
        public static readonly BelowEightyConverter Instance = new BelowEightyConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return d < 80.0 && d > 0.0;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}