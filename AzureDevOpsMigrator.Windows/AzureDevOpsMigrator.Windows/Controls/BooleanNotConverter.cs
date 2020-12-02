using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDevOpsMigrator.Windows.Controls
{
    public class BooleanNotConverter : IValueConverter
    {
        private object Convert(object value)
        {
            if (!(value is bool))
            {
                throw new InvalidCastException($"{value.ToString()} is not a boolean.");
            }
            return !((bool)value);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Convert(value);

        public object Convert(object value, Type targetType, object parameter, string language) => Convert(value);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Convert(value);

        public object ConvertBack(object value, Type targetType, object parameter, string language) => Convert(value);
    }
}
