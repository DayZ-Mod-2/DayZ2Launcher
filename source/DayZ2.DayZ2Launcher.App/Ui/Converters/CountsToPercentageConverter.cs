using System;
using System.Globalization;
using System.Windows.Data;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
    public class CountsToPercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var count = (int)values[0];
            var totalCount = (int)values[1];
            decimal percentage = (count / (decimal)totalCount);
            decimal roundedPercentage = Math.Round(percentage * 100);
            if (roundedPercentage == 0)
                return "< 1%";
            return roundedPercentage + "%";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}