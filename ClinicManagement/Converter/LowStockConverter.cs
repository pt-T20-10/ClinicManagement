using System.Globalization;
using System.Windows.Data;

namespace ClinicManagement.Converter
{
    public class LowStockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stockQuantity)
            {
                return stockQuantity < 10; // Return true when stock is low
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
