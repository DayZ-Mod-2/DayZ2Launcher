using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using DayZ2.DayZ2Launcher.App.UI.ServerList;
using UserControl = System.Windows.Controls.UserControl;

namespace DayZ2.DayZ2Launcher.App.Ui.ServerList
{
	/// <summary>
	///     Interaction logic for ServerListView.xaml
	/// </summary>
	public partial class ServerListView : UserControl
	{
		ServerListViewModel ViewModel => (ServerListViewModel)DataContext;

		public ServerListView()
		{
			InitializeComponent();
		}

		private void ServerDoubleClick(object sender, MouseButtonEventArgs e)
		{
			((ServerViewModel)((ContentControl)sender).DataContext).Join();
		}

		private void RowKeyDown(object sender, RoutedEventArgs e)
		{
		}

		private void RowKeyUp(object sender, RoutedEventArgs e)
		{
		}

		private void ServerDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs args)
		{
			((DataGrid)sender).UnselectAllCells();
		}

		private void RefreshAll_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.RefreshAll();
		}
	}
}
