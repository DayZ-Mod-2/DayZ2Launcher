using System.Linq;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using DayZ2.DayZ2Launcher.App.Core;
using DayZ2.DayZ2Launcher.App.Ui.Recent;

namespace DayZ2.DayZ2Launcher.App.Ui.Controls
{
	/// <summary>
	///     Interaction logic for ServerStandaloneHeaderRow.xaml
	/// </summary>
	public partial class ServerStandaloneHeaderRow :
		IHandle<RefreshingServersChange>
	{
		public ServerStandaloneHeaderRow()
		{
			InitializeComponent();
			App.Events.Subscribe(this);
		}

		public void Handle(RefreshingServersChange message)
		{
			Execute.OnUiThread(() =>
			{
				RefreshAllButton.Visibility = message.IsRunning
					? Visibility.Hidden
					: Visibility.Visible;
			}, Dispatcher);
		}

		private void RefreshAllServer(object sender, RoutedEventArgs e)
		{
			var recent = DataContext as RecentViewModel;
			if (recent != null)
			{
				var batch = new ServerBatchRefresher("Refreshing recent servers...", recent.Servers.Select(r => r.Server).ToList());
				App.Events.Publish(new RefreshServerRequest(batch));
			}
		}

		private void RefreshAllServersDoubleClick(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}
	}
}
