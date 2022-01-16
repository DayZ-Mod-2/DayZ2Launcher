using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	class UpdateStatusToStyleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string suffix = "Red";
			if (value != null)
			{
				UpdatesViewModel.UpdateInfo status = (UpdatesViewModel.UpdateInfo)value;
				switch (status.Status)
				{
					case UpdateStatus.UpToDate:
						suffix = "LightGray";
						break;
					case UpdateStatus.OutOfDate:
						suffix = "Yellow";
						break;
					case UpdateStatus.Checking:
						suffix = "LightGreen";
						break;
					case UpdateStatus.Error:
						suffix = "Red";
						break;
				}
			}

			string baseStyleName = "MetroTextButtonStyle";
			if (parameter != null)
				baseStyleName = (string)parameter;

			var newStyle = (Style)Application.Current.TryFindResource(baseStyleName + suffix);
			return newStyle;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
