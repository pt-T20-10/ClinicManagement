using System.Windows.Data;

namespace ClinicManagement.Converter
{
    public class ExpiredConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateOnly expiryDate)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                
                return expiryDate <= today;
            }
            return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
