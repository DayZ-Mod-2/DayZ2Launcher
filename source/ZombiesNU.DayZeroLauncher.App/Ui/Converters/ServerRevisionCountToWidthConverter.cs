using System;
using System.Globalization;
using System.Windows.Data;

namespace zombiesnu.DayZeroLauncher.App.Ui.Converters
{
	public class ServerRevisionCountToWidthConverter : IMultiValueConverter
	{
		public double MaxWidth = 140;

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var count = (int) values[0];
			var totalCount = (int) values[1];
			decimal percentage = (count/(decimal) totalCount);
			return (double) Math.Floor((decimal) MaxWidth*percentage);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}