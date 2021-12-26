﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
    public class VersionMatchesToForegroundConverter : IValueConverter
    {
        public static SolidColorBrush IsSame = new SolidColorBrush(Colors.AliceBlue);
        public static SolidColorBrush IsDifferent = new SolidColorBrush(Color.FromArgb(255, 153, 153, 153));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return IsSame;
            }
            return IsDifferent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}