﻿using System.Windows.Controls;

namespace zombiesnu.DayZeroLauncher.App.Ui.ServerList
{
	/// <summary>
	/// Interaction logic for ServerListView.xaml
	/// </summary>
	public partial class ServerListView : UserControl
	{
		public ServerListView()
		{
			InitializeComponent();
		}

		public ServerListViewModel ViewModel() { return (ServerListViewModel)DataContext; }

        private void ServerListGrid_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void ServerListGrid_Loaded_1(object sender, System.Windows.RoutedEventArgs e)
        {

        }
	}
}
