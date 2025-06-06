using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClinicManagement.Models;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace ClinicManagement.SubWindow
{
    // SimpleMultiplicationConverter for calculating SalePrice * Quantity in DataGrid
    public class SimpleMultiplicationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = SalePrice
            // values[1] = Quantity

            if (values.Length < 2 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return 0;

            decimal price = 0;
            int quantity = 1;

            if (values[0] != null && decimal.TryParse(values[0].ToString(), out decimal parsedPrice))
                price = parsedPrice;

            if (values[1] != null && int.TryParse(values[1].ToString(), out int parsedQuantity))
                quantity = parsedQuantity;

            return price * quantity;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}