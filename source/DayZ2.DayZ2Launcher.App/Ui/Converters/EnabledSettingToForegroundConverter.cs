using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	public class PerspectiveToForegroundConverter : IValueConverter
	{
		private static readonly SolidColorBrush Empty = new SolidColorBrush(Colors.Transparent);
		private static readonly SolidColorBrush Enabled = new SolidColorBrush(Color.FromArgb(255, 238, 238, 238));
		private static readonly SolidColorBrush Disabled = new SolidColorBrush(Color.FromArgb(255, 87, 87, 87));

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				switch ((ServerPerspective)value)
				{
					case ServerPerspective.FirstPerson:
						return Disabled;
					case ServerPerspective.ThirdPerson:
						return Enabled;
				}
			}
			return Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
