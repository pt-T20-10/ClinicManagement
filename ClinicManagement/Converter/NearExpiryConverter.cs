using System.Globalization;
using System.Windows.Data;


    namespace ClinicManagement.Converter
    {
        public class NearExpiryConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is DateOnly expiryDate)
                {
                    // Check if expiry date is within 8 days
                    var today = DateOnly.FromDateTime(DateTime.Today);

                    // Calculate days until expiry
                    var daysToExpiry = expiryDate.DayNumber - today.DayNumber;

                    // Return true only if:
                    // 1. The expiry date is in the future (not already expired)
                    // 2. AND the expiry date is less than 8 days away
                    return daysToExpiry >= 0 && daysToExpiry < 8;
                }
                return false;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }


