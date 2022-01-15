using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
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

		private void RowDoubleClick(object sender, RoutedEventArgs e)
		{
			((ServerViewModel)((DataGridRow)sender).DataContext).Join();
		}

		private void RowKeyDown(object sender, RoutedEventArgs e)
		{
		}

		private void RowKeyUp(object sender, RoutedEventArgs e)
		{
		}

		private void PreviewMouseLeftButtonDown(object sender, RoutedEventArgs e)
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
