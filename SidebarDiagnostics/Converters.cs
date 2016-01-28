using System;
using System.Globalization;
using System.Windows.Data;

namespace SidebarDiagnostics.Converters
{
    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string _format = (string)parameter;

            if (string.IsNullOrEmpty(_format))
            {
                return value.ToString();
            }
            else
            {
                return string.Format(culture, _format, value);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int _return = 0;

            int.TryParse(value.ToString(), out _return);

            return _return;
        }
    }
}
