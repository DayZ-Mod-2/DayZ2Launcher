using System.Windows.Controls;
using System.Windows.Input;
using zombiesnu.DayZeroLauncher.App.Core;

namespace zombiesnu.DayZeroLauncher.App.Ui.Recent
{
	/// <summary>
	///     Interaction logic for RecentView.xaml
	/// </summary>
	public partial class RecentView : UserControl
	{
		public RecentView()
		{
			InitializeComponent();
		}

		private void RowDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var recentServer = (RecentServer) TheList.SelectedItem;
			if (recentServer == null)
				return;

			//GameLauncher.JoinServer(MainWindow.GetWindow(this.Parent),recentServer.Server);
		}
	}
}