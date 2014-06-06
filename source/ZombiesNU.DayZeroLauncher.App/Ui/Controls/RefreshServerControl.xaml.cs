using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using zombiesnu.DayZeroLauncher.App.Core;

namespace zombiesnu.DayZeroLauncher.App.Ui.Controls
{
	/// <summary>
	///     Interaction logic for RefreshServerControl.xaml
	/// </summary>
	public partial class RefreshServerControl : UserControl
	{
		public RefreshServerControl()
		{
			InitializeComponent();
		}

		private void RefreshServer(object sender, RoutedEventArgs e)
		{
			var server = (Server) ((Control) sender).DataContext;
			server.BeginUpdate(server1 => { }, true);
		}

		private void RefreshServerDoubleClick(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}
	}
}