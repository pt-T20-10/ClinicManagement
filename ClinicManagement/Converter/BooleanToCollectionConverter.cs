using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace ClinicManagement.Converter
{
    public class BooleanToCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Handle null value early
            if (value == null)
                return null;

            bool isActive;
            if (value is bool boolValue)
                isActive = boolValue;
            else
                return null; // Can't convert non-boolean values

            // Check parameter
            if (!(parameter is string paramString) || string.IsNullOrEmpty(paramString))
                return null;

            string[] paramStrings = paramString.Split(';');
            if (paramStrings.Length != 2)
                return null; // Need exactly two property names

            // Get the source object directly from the Application resources
            object source = null;

            try
            {
                // First approach - check if isActive itself is a FrameworkElement with DataContext
                if (value is FrameworkElement fe && fe.DataContext != null)
                {
                    source = fe.DataContext;
                }
                // Second approach - try to find the main view model from Application resources
                else if (Application.Current != null)
                {
                    // Check if we can find the ViewModel in resources
                    // Common keys to search for view models
                    string[] possibleResourceKeys = {
                        "MainVM", "MainViewModel",
                        "StockVM", "StockMedicineVM",
                        "StockViewModel", "StockMedicineViewModel"
                    };

                    foreach (string key in possibleResourceKeys)
                    {
                        if (Application.Current.Resources.Contains(key))
                        {
                            source = Application.Current.Resources[key];
                            if (source != null)
                                break;
                        }
                    }

                    // If still not found, try to get from main window's DataContext
                    if (source == null && Application.Current.MainWindow != null)
                    {
                        source = Application.Current.MainWindow.DataContext;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue execution
                Console.WriteLine($"Error finding converter source: {ex.Message}");
            }

            if (source == null)
                return null; // Couldn't find a valid source

            try
            {
                // Get the property value based on boolean flag
                string propertyName = isActive ? paramStrings[0] : paramStrings[1];
                PropertyInfo propertyInfo = source.GetType().GetProperty(propertyName);

                if (propertyInfo != null)
                    return propertyInfo.GetValue(source);
            }
            catch (Exception ex)
            {
                // Log error but continue execution
                Console.WriteLine($"Error accessing property: {ex.Message}");
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
