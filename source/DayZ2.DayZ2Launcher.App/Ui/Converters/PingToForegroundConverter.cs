using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	public class PingToForegroundConverter : IValueConverter
	{
		private static SolidColorBrush Nothing = new SolidColorBrush(Color.FromArgb(255, 204, 204, 204));
		private static SolidColorBrush Fastest = new SolidColorBrush(Color.FromArgb(255, 25, 253, 25));
		private static SolidColorBrush Fast = new SolidColorBrush(Color.FromArgb(255, 120, 239, 120));
		private static SolidColorBrush Medium = new SolidColorBrush(Colors.Yellow);
		private static SolidColorBrush Slow = new SolidColorBrush(Colors.Orange);
		private static SolidColorBrush Slowest = new SolidColorBrush(Colors.Red);

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return Nothing;

			var val = (long)value;
			if (val < 60)
				return Fastest;
			if (val < 120)
				return Fast;
			if (val < 180)
				return Medium;
			if (val < 240)
				return Slow;

			return Slowest;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
