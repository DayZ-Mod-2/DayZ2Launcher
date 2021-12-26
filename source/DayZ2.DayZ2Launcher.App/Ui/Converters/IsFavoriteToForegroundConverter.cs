﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
    public class IsFavoriteToForegroundConverter : IValueConverter
    {
        public static SolidColorBrush True = new SolidColorBrush(Colors.Yellow);
        public static SolidColorBrush False = new SolidColorBrush(Color.FromArgb(255, 136, 136, 136));


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return True;
            return False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}