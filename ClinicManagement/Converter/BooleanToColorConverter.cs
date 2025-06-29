using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClinicManagement.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isTrue)
            {
                // Mặc định: True = Blue, False = Transparent
                string trueColor = "#E3F2FD";  // Light Blue
                string falseColor = "Transparent";

                // Nếu có tham số chỉ định màu sắc (format: "TrueColor|FalseColor")
                if (parameter is string colorParam)
                {
                    var colors = colorParam.Split('|');
                    if (colors.Length >= 2)
                    {
                        trueColor = colors[0];
                        falseColor = colors[1];
                    }
                }

                var colorStr = isTrue ? trueColor : falseColor;
                return new BrushConverter().ConvertFromString(colorStr);
            }

            return new BrushConverter().ConvertFromString("Transparent");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
