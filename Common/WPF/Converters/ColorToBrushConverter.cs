using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Common.WPF.Converters
{
	public class ColorToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			switch (value.ToString())
			{
				case "Красный":
					return new SolidColorBrush(Colors.Crimson);
				case "Желтый":
					return new SolidColorBrush(Colors.Yellow);
				case "Зелёный":
					return new SolidColorBrush(Colors.Green);
				case "Голубой":
					return new SolidColorBrush(Colors.SkyBlue);
				case "Синий":
					return new SolidColorBrush(Colors.Blue);
				case "Фиолетовый":
					return new SolidColorBrush(Colors.Violet);
				case "Белый":
					return new SolidColorBrush(Colors.White);
				default:
                    if (value.ToString() != "")
                    {
                        return (SolidColorBrush)new BrushConverter().ConvertFrom(value.ToString());
                    }
                    return new SolidColorBrush(Colors.Black);
            }
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}