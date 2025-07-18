using ClinicManagement.SubWindow;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClinicManagement.Converter
{
    public class MessageTypeToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageType messageType)
            {
                switch (messageType)
                {
                    case MessageType.Information:
                        return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
                    case MessageType.Warning:
                        return new SolidColorBrush(Color.FromRgb(255, 152, 0));  // Orange
                    case MessageType.Error:
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54));  // Red
                    case MessageType.Success:
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80));  // Green
                    case MessageType.Question:
                        return new SolidColorBrush(Color.FromRgb(156, 39, 176)); // Purple
                }
            }

            return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Default blue
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageType messageType)
            {
                switch (messageType)
                {
                    case MessageType.Information:
                        return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M13,7H11V13H13V7M13,15H11V17H13V15Z"; // Info icon
                    case MessageType.Warning:
                        return "M13,14H11V10H13M13,18H11V16H13M1,21H23L12,2L1,21Z"; // Warning icon
                    case MessageType.Error:
                        return "M13,13H11V7H13M13,17H11V15H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"; // Error icon
                    case MessageType.Success:
                        return "M9,16.17L4.83,12l-1.42,1.41L9,19 21,7l-1.41-1.41L9,16.17z"; // Checkmark
                    case MessageType.Question:
                        return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M13,7H11V13H13V7M13,15H11V17H13V15Z"; // Question mark
                }
            }

            return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M13,7H11V13H13V7M13,15H11V17H13V15Z"; // Default info icon
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
