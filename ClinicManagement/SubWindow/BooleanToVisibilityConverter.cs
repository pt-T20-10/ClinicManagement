using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClinicManagement.SubWindow
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        ///// <summary>
        ///// Converts a boolean value to a Visibility value.
        ///// </summary>
        ///// <param name="value">The boolean value to convert.</param>
        ///// <param name="targetType">The type of the target property.</param>
        ///// <param name="parameter">Optional parameter to invert the conversion.</param>
        ///// <param name="culture">The culture to use in the converter.</param>
        ///// <returns>Visibility.Visible if value is true; otherwise Visibility.Collapsed.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;

            // Handle different types that can be converted to boolean
            if (value is bool)
            {
                boolValue = (bool)value;
            }
            else if (value is int)
            {
                boolValue = (int)value != 0;
            }
            else if (value is string)
            {
                bool.TryParse(value as string, out boolValue);
            }

            // Check if we need to invert the result
            bool invertResult = false;
            if (parameter != null)
            {
                bool.TryParse(parameter.ToString(), out invertResult);
            }

            // Return the appropriate Visibility value
            if (invertResult)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        ///// <summary>
        ///// Converts a Visibility value to a boolean value.
        ///// </summary>
        ///// <param name="value">The Visibility value to convert.</param>
        ///// <param name="targetType">The type of the target property.</param>
        ///// <param name="parameter">Optional parameter to invert the conversion.</param>
        ///// <param name="culture">The culture to use in the converter.</param>
        ///// <returns>true if value is Visibility.Visible; otherwise false.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the value is not a Visibility, return false
            if (!(value is Visibility))
            {
                return false;
            }

            bool result = (Visibility)value == Visibility.Visible;

            // Check if we need to invert the result
            bool invertResult = false;
            if (parameter != null)
            {
                bool.TryParse(parameter.ToString(), out invertResult);
            }

            return invertResult ? !result : result;
        }
    }
}
