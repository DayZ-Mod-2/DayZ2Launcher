using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using zombiesnu.DayZeroLauncher.App.Core;

namespace zombiesnu.DayZeroLauncher.App.Ui.Converters
{
	public class UpdateStatusToStyleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string suffix = "Red";
			if (value != null)
			{
				string realVal = (string)value;

				if (realVal.StartsWith(DayZeroLauncherUpdater.STATUS_CHECKINGFORUPDATES.Replace("...", String.Empty)))
					suffix = "LightGreen";
				else if (realVal.StartsWith(DayZeroLauncherUpdater.STATUS_DOWNLOADING.Replace("...", String.Empty)))
					suffix = "LightGreen";
				else if (realVal == DayZeroLauncherUpdater.STATUS_UPDATEREQUIRED || realVal == DayZeroLauncherUpdater.STATUS_OUTOFDATE)
					suffix = "Yellow";
				else if (realVal == DayZeroLauncherUpdater.STATUS_UPTODATE)
					suffix = "LightGray";
			}

			string baseStyleName = "MetroTextButtonStyle";
			if (parameter != null)
				baseStyleName = (string)parameter;

			Style newStyle = (Style)Application.Current.TryFindResource(baseStyleName + suffix);
			return newStyle;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}