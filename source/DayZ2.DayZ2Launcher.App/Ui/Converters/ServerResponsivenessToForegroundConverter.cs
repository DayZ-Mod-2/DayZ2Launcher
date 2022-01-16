using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
	public class ServerResponsivenessToForegroundConverter : IValueConverter
	{
		private static SolidColorBrush Reponding = new (Colors.AliceBlue);
		private static SolidColorBrush NotResponding = new (Color.FromArgb(255, 153, 153, 153));

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				return (bool)value ? Reponding : NotResponding;
			}

			return NotResponding;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
