using System.Globalization;
using System.Windows.Data;

namespace ClinicManagement.Converter
{
    public class MonthlyStockValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 &&
                values[0] is decimal unitPrice &&
                values[1] is int quantity)
            {
                return unitPrice * quantity;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
