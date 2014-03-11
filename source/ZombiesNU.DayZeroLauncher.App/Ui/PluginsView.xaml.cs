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
	/// Interaction logic for PluginsView.xaml
	/// </summary>
	public partial class PluginsView : UserControl
	{
		public PluginsView()
		{
			InitializeComponent();
		}

		private PluginsViewModel ViewModel() { return (PluginsViewModel)DataContext; }

		private void Done_Click(object sender, RoutedEventArgs e)
		{
			ViewModel().IsVisible = false;
		}

		private void MissingPluginsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var dg = (DataGrid)sender;
			dg.UnselectAllCells();
		}

		private void MissingPlugin_Click(object sender, RoutedEventArgs e)
		{
			var fe = (FrameworkElement)sender;
			var pe = (MetaPlugin)fe.DataContext;
			ViewModel().RemoveMissing(pe);
		}

		private void PluginEnabledCheckBoxChanged(object sender, RoutedEventArgs e)
		{
			ViewModel().SaveSettings();
		}
	}
}
