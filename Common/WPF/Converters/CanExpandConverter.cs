using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Common.WPF.Converters
{
	public class CanExpandConverter : IValueConverter
	{
		public object Convert(object obj, Type type, object parameter, CultureInfo culture)
		{
			return (bool)obj ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object obj, Type type, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}