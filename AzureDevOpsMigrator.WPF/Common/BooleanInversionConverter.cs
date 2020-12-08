using System;
using System.Windows.Data;

namespace AzureDevOpsMigrator.WPF
{
    public class BooleanInversionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return bool.Parse(parameter.ToString());
            }
            return !((bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return bool.Parse(parameter.ToString());
            }
            return !((bool)value);
        }
    }
}
