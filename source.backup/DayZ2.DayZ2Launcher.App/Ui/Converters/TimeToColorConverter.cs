using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	public class TimeToColorConverter : IValueConverter
	{
		private static SolidColorBrush Night = new SolidColorBrush(Color.FromArgb(255, 171, 171, 171));
		private static SolidColorBrush Day = new SolidColorBrush(Colors.Yellow);

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var dateTime = value as DateTime?;
			if (value == null)
				return Day;

			if (dateTime.Value.Hour < 5 || dateTime.Value.Hour > 19)
			{
				return Night;
			}

			return Day;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
