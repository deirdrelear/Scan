using System;
using System.Globalization;
using System.Windows.Data;

namespace Common.WPF.Converters
{
	public class InverseBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((bool)value)
			{
				return false;
			}
			return true;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((bool)value)
			{
				return false;
			}
			return true;
		}
	}
}
