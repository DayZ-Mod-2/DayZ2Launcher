using System;
using System.Collections.Generic;
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

namespace zombiesnu.DayZeroLauncher.App.Ui
{
	/// <summary>
	/// Interaction logic for SettingsView.xaml
	/// </summary>
	public partial class SettingsView : UserControl
	{
		public SettingsView()
		{
			InitializeComponent();
		}

		private void BrowseA2_Click(object sender, RoutedEventArgs e)
		{
			string foundDir = ViewModel.DisplayDirectoryPrompt(MainWindow.GetWindow(this.Parent),false,ViewModel.Arma2Directory,"Locate ArmA2 game directory");
			if (foundDir != null)
				ViewModel.Arma2Directory = foundDir;
		}

		private void BrowseA2OA_Click(object sender, RoutedEventArgs e)
		{
			string foundDir = ViewModel.DisplayDirectoryPrompt(MainWindow.GetWindow(this.Parent), false, ViewModel.Arma2OADirectory, "Locate Operation Arrowhead game directory");
			if (foundDir != null)
				ViewModel.Arma2OADirectory = foundDir;
		}

		private void BrowseAddons_Click(object sender, RoutedEventArgs e)
		{
			string foundDir = ViewModel.DisplayDirectoryPrompt(MainWindow.GetWindow(this.Parent), true, ViewModel.AddonsDirectory, "Directory in which mods will be installed");
			if (foundDir != null)
				ViewModel.AddonsDirectory = foundDir;
		}

		private void Done_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Done();
		}

		protected SettingsViewModel ViewModel
		{
			get { return (SettingsViewModel)DataContext; }
		}
	}
}
