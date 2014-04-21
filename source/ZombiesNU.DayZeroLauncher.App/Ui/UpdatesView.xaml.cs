using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using zombiesnu.DayZeroLauncher.App.Core;
using zombiesnu.DayZeroLauncher.App.Ui.Controls;
using MonoTorrent.Common;

namespace zombiesnu.DayZeroLauncher.App.Ui
{
	/// <summary>
	/// Interaction logic for UpdatesView.xaml
	/// </summary>
	public partial class UpdatesView : UserControl
	{
		public UpdatesView()
		{
			InitializeComponent();
		}

		private UpdatesViewModel ViewModel() { return (UpdatesViewModel)DataContext; }

		private void ApplyLauncherUpdate_Click(object sender, RoutedEventArgs e)
		{
			ViewModel().DayZeroLauncherUpdater.UpdateToLatest();
		}

		private void RestartDayZeroLauncher_Click(object sender, RoutedEventArgs e)
		{
			ViewModel().DayZeroLauncherUpdater.RestartNewVersion();
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
