using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace AzureDevOpsMigrator.Windows.Controls
{
    public class StringTemplateConverter : IValueConverter
    {
        private object Convert(object value, object template)
        {
            if (!(template is string))
            {
                throw new InvalidCastException($"{template} is not a valid string template.");
            }
            return string.Format((string)template, value);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Convert(value, parameter);

        public object Convert(object value, Type targetType, object parameter, string language) => Convert(value, parameter);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Convert(value, parameter);

        public object ConvertBack(object value, Type targetType, object parameter, string language) => Convert(value, parameter);
    }
}
