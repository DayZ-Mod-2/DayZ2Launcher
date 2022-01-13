using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DayZ2.DayZ2Launcher.App.Core;

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
			var server = (Server)value;
			if (server == null)
				return null;

			if (server.FreeSlots == 0)
				return Full;
			if (server.FreeSlots < 5)
				return NearFull;
			if (server.MaxPlayers - server.FreeSlots < 3)
				return Empty;
			if (server.FreeSlots >= 5)
				return SomeSpace;

			return Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
