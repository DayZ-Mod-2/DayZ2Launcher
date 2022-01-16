using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DayZ2.DayZ2Launcher.App.Core;
using DayZ2.DayZ2Launcher.App.UI.ServerList;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	public class ServerFullnessToColorConverter : IValueConverter
	{
		private static SolidColorBrush Full = new SolidColorBrush(Colors.Red);
		private static SolidColorBrush NearFull = new SolidColorBrush(Colors.Yellow);
		private static SolidColorBrush SomeSpace = new SolidColorBrush(Colors.LightGreen);
		private static SolidColorBrush Empty = new SolidColorBrush(Color.FromArgb(255, 171, 171, 171));

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var (playerCount, slots) = (Rational)value;
			var freeSlots = slots - playerCount;

			if (freeSlots == 0)
				return Full;
			if (freeSlots < 5)
				return NearFull;
			if (playerCount < 3)
				return Empty;
			if (freeSlots >= 5)
				return SomeSpace;

			return Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
