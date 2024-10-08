﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	public class NonZeroToVisibilityHiddenConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				if ((int)value > 0)
					return Visibility.Visible;

				return Visibility.Hidden;
			}
			return Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
