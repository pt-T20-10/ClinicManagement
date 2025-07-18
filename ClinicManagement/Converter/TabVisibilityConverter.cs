using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClinicManagement.Converter
{
    public class TabVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is ObservableCollection<string> visibleTabs && parameter is string tabName)
                {
                    // Loại bỏ khoảng trắng dư thừa và so sánh
                    string cleanTabName = tabName.Trim();
                    bool isVisible = false;

                    // Kiểm tra xem tab có trong danh sách hiển thị không
                    foreach (string tab in visibleTabs)
                    {
                        if (string.Equals(tab.Trim(), cleanTabName, StringComparison.OrdinalIgnoreCase))
                        {
                            isVisible = true;
                            break;
                        }
                    }

                    // Debug
                    Console.WriteLine($"Tab '{cleanTabName}' is {(isVisible ? "visible" : "collapsed")}");

                    return isVisible ? Visibility.Visible : Visibility.Collapsed;
                }

                return Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TabVisibilityConverter: {ex.Message}");
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }



}
