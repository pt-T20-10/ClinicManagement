using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ClinicManagement.Models;

namespace ClinicManagement.Converter
{
    public class ItemNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = ServiceName
            // values[1] = Medicine.Name
            // values[2] = MedicineId (to check if this is a medicine item)

            // If ServiceName is provided and not empty, use it (for services)
            if (values[0] != null && values[0] != DependencyProperty.UnsetValue && !string.IsNullOrEmpty(values[0].ToString()))
            {
                return values[0].ToString();
            }

            // Otherwise, try to use Medicine.Name if this is a medicine item
            if (values[2] != null && values[2] != DependencyProperty.UnsetValue &&
                values[1] != null && values[1] != DependencyProperty.UnsetValue)
            {
                return values[1].ToString();
            }

            // If we get here, check if the first parameter has a meaningful default
            if (values.Length > 0 && values[0] == null)
            {
                return "Dịch vụ khám bệnh";
            }

            return "Không xác định";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
