using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	public class BooleanToSelectedOptionColorConverter : IValueConverter
	{
		private static SolidColorBrush Selected = new(Colors.White);
		private static SolidColorBrush NotSelected = new(Color.FromArgb(255, 170, 170, 170));

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				return (bool)value ? Selected : NotSelected;
			}

			return NotSelected;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
