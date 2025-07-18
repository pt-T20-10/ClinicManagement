using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClinicManagement.Converter
{
    public class AppointmentStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                if (status == "Đang chờ" || status == "Đang khám" || status == "Đã khám" || status == "Đã hủy")
                {
                    return Visibility.Collapsed;
                }
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
