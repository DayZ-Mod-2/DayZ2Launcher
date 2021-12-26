using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.Converters
{
    public class UpdateStatusToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string suffix = "Red";
            if (value != null)
            {
                var realVal = (string)value;

                if (realVal.StartsWith(DayZLauncherUpdater.STATUS_CHECKINGFORUPDATES.Replace("...", String.Empty)))
                    suffix = "LightGreen";
                else if (realVal.StartsWith(DayZLauncherUpdater.STATUS_DOWNLOADING.Replace("...", String.Empty)))
                    suffix = "LightGreen";
                else if (realVal == DayZLauncherUpdater.STATUS_UPDATEREQUIRED || realVal == DayZLauncherUpdater.STATUS_OUTOFDATE)
                    suffix = "Yellow";
                else if (realVal == DayZLauncherUpdater.STATUS_UPTODATE)
                    suffix = "LightGray";
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