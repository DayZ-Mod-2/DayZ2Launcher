using System.Windows;
using System.Windows.Controls;

namespace DayZ2.DayZ2Launcher.App.Ui.ServerList
{
	/// <summary>
	///     Interaction logic for ServerListView.xaml
	/// </summary>
	public partial class ServerListView : UserControl
	{
		public ServerListView()
		{
			InitializeComponent();
		}

		public ServerListViewModel ViewModel()
		{
			return (ServerListViewModel)DataContext;
		}

		private void ServerListGrid_Loaded(object sender, RoutedEventArgs e)
		{
		}

		private void ServerListGrid_Loaded_1(object sender, RoutedEventArgs e)
		{
		}
	}
}
