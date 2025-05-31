using System;
using System.Globalization;
using System.Windows.Data;

namespace ClinicManagement.SubWindow
{
    public class BooleanToPharmacyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPharmacySale)
            {
                return isPharmacySale ? "Bán thuốc" : "Dịch vụ khám";
            }
            return "Không xác định";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}