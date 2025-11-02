using System;
using System.Globalization;
using System.Windows.Data;

namespace Common.WPF.Converters
{
	/// <summary>
	/// Convert Level to left margin
	/// Pass a prarameter if you want a unit length other than default.
	/// </summary>
	public class LevelToIndentConverter : IValueConverter
	{
		public object Convert(object o, Type type, object parameter, CultureInfo culture)
		{
			Double indentSize = 5;
			if (parameter != null)
				Double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out indentSize);

			return (int)o * indentSize + " 0 0 0";
		}

		public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}