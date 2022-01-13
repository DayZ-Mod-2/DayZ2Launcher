using System.Windows;
using System.Windows.Controls;

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

		UpdatesViewModel ViewModel => (UpdatesViewModel)DataContext;

		private void ApplyLauncherUpdate_Click(object sender, RoutedEventArgs e)
		{
			//ViewModel.m_launcherUpdater.UpdateToLatest();
		}

		private void RestartDayZLauncher_Click(object sender, RoutedEventArgs e)
		{
			//ViewModel.m_launcherUpdater.RestartNewVersion();
		}

		public void CheckForUpdates()
		{
			CheckNow_Click(this, null);
		}

		private void InstallLatestVersion_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.InstallLatestModVersion();
		}

		private void VerifyIntegrity_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.VerifyIntegrity();
		}

		private void CheckNow_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.CheckForUpdates();
		}

		private void Done_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.IsVisible = false;
		}
	}
}
