using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	public class EnabledSettingToForegroundConverter : IValueConverter
	{
		private static SolidColorBrush Empty = new SolidColorBrush(Colors.Transparent);
		private static SolidColorBrush Enabled = new SolidColorBrush(Color.FromArgb(255, 238, 238, 238));
		private static SolidColorBrush Disabled = new SolidColorBrush(Color.FromArgb(255, 87, 87, 87));

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				if ((bool)value)
					return Enabled;
				else
					return Disabled;
			}
			return Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
