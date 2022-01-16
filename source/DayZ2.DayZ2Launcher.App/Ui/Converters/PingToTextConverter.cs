using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	public class PingToTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				return $"{(long?)value} ms";
			}

			return "?";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
