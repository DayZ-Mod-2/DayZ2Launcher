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
        private static readonly SolidColorBrush ActionRequired = new SolidColorBrush(Colors.Yellow);
        private static readonly SolidColorBrush OK = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221));
        private static readonly SolidColorBrush Default = new SolidColorBrush(Colors.Red);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var realVal = (string)value;

                if (realVal.StartsWith(DayZLauncherUpdater.STATUS_CHECKINGFORUPDATES.Replace("...", String.Empty)))
                    return InProgress;
                if (realVal.StartsWith(DayZLauncherUpdater.STATUS_DOWNLOADING.Replace("...", String.Empty)))
                    return InProgress;
                if (realVal == DayZLauncherUpdater.STATUS_UPDATEREQUIRED || realVal == DayZLauncherUpdater.STATUS_OUTOFDATE)
                    return ActionRequired;
                if (realVal == DayZLauncherUpdater.STATUS_UPTODATE)
                    return OK;
            }

            return Default;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}