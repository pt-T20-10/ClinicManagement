using System;
using System.Globalization;
using System.Windows.Data;

namespace ClinicManagement.Converter
{
    public class GreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return doubleValue > 0;
            }
            else if (value is decimal decimalValue)
            {
                return decimalValue > 0;
            }
            else if (value is float floatValue)
            {
                return floatValue > 0;
            }
            else if (value is int intValue)
            {
                return intValue > 0;
            }
            else if (value is string stringValue && double.TryParse(stringValue, out double parsedValue))
            {
                return parsedValue > 0;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
