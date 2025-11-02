using System;
using System.Windows.Data;

namespace LocalScan.WPF.Converters
{
    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = value as string;
            if (string.IsNullOrEmpty(str))
                return 0;

            int.TryParse(str, out int res);

            return res;
        }
    }
}
