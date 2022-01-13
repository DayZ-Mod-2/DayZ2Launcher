using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	public class UpdateStatusToTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				UpdatesViewModel.UpdateInfo status = (UpdatesViewModel.UpdateInfo)value;
				switch (status.Status)
				{
					case UpdateStatus.UpToDate:
						return "Up To Date";
					case UpdateStatus.OutOfDate:
						return "Out Of Date";
					case UpdateStatus.Checking:
						return "Checking";
					case UpdateStatus.Error:
						return $"Error: {status.Text}";
				}
			}

			return "Error";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
