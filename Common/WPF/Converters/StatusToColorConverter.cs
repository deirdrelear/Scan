using Common.Helpers;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Common.WPF.Converters
{
	public class StatusToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			switch ((Status)value)
			{
               
                case Status.Default:
					return new SolidColorBrush(Colors.DarkOrange);
				case Status.ExportInProgress:            
                    return new SolidColorBrush(Colors.DeepPink);
                case Status.Warning:
                    return new SolidColorBrush(Colors.Gold);
                case Status.SpecialWarning:
                    return new SolidColorBrush(Colors.Orange);
                case Status.Error:
                case Status.OperationFailed:
					return new SolidColorBrush(Colors.Crimson);
                case Status.OperationSuccess:
					return new SolidColorBrush(Colors.LimeGreen);
                case Status.Normal:
                    return new SolidColorBrush(Colors.Transparent);
                default:
					return new SolidColorBrush(Colors.Crimson);
			}

		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
