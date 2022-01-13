using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	public class UpdateStatusToForegroundConverter : IValueConverter
	{
		private static readonly SolidColorBrush InProgress = new SolidColorBrush(Color.FromArgb(255, 7, 132, 181));
		private static readonly SolidColorBrush OutOfDate = new SolidColorBrush(Colors.Yellow);
		private static readonly SolidColorBrush UpToDate = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221));
		private static readonly SolidColorBrush Error = new SolidColorBrush(Colors.Red);

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				UpdatesViewModel.UpdateInfo status = (UpdatesViewModel.UpdateInfo)value;
				switch (status.Status)
				{
					case UpdateStatus.UpToDate:
						return UpToDate;
					case UpdateStatus.OutOfDate:
						return OutOfDate;
					case UpdateStatus.Checking:
						return InProgress;
					case UpdateStatus.Error:
						return Error;
				}
			}

			return Error;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
