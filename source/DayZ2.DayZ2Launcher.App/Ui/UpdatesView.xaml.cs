using System.Windows;
using System.Windows.Controls;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui
{
    /// <summary>
    ///     Interaction logic for UpdatesView.xaml
    /// </summary>
    public partial class UpdatesView : UserControl
    {
        public UpdatesView()
        {
            InitializeComponent();
        }

        private UpdatesViewModel ViewModel()
        {
            return (UpdatesViewModel)DataContext;
        }

        private void ApplyLauncherUpdate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel().DayZLauncherUpdater.UpdateToLatest();
        }

        private void RestartDayZLauncher_Click(object sender, RoutedEventArgs e)
        {
            ViewModel().DayZLauncherUpdater.RestartNewVersion();
        }

        private void InstallLatestPatch_Click(object sender, RoutedEventArgs e)
        {
            ViewModel().Arma2Updater.InstallLatestVersion(this);
        }

        public void CheckForUpdates()
        {
            CheckNow_Click(this, null);
        }

        private void InstallLatestVersion_Click(object sender, RoutedEventArgs e)
        {
            ViewModel().DayZUpdater.DownloadLatestVersion(false);
        }

        private void FullSystemCheck_Click(object sender, RoutedEventArgs e)
        {
            ViewModel().DayZUpdater.DownloadLatestVersion(true);
        }

        private void ClearModDir_Click(object sender, RoutedEventArgs e)
        {
            ViewModel().DayZUpdater.ClearModDir();
        }

        private void CheckNow_Click(object sender, RoutedEventArgs e)
        {
            ViewModel().CheckForUpdates();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            ViewModel().IsVisible = false;
        }
    }
}
