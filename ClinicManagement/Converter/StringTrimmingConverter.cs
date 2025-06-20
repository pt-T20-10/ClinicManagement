using System;
using System.Globalization;
using System.Windows.Data;

namespace ClinicManagement.Converter
{
    public class StringTrimmingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return stringValue.Trim();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return stringValue.Trim();
            }
            return value;
        }
    }
}
