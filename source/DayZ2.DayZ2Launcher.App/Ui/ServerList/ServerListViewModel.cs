using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.ServerList
{
	public class ServerListViewModel : ViewModelBase
	{
		private readonly CancellationToken m_cancellationToken;

		readonly IServiceProvider m_serviceProvider;
		readonly GameLauncher m_gameLauncher;
		readonly Core.ServerList m_serverList = new();

		public ServerListViewModel(IServiceProvider services, GameLauncher gameLauncher, AppCancellation cancellation)
		{
			m_serviceProvider = services;
			m_gameLauncher = gameLauncher;
			m_cancellationToken = cancellation.Token;

			Title = "Servers";

			FiltersViewModel = new FiltersViewModel();
			ListViewModel = new ListViewModel();

			m_serverList.ServerDiscovered += (object sender, ServerDiscoveredEventArgs e) =>
			{
				//Servers.Add(new ServerViewModel(gameLauncher, e.Server, m_cancellationToken));
				Servers.Add(m_serviceProvider.CreateInstance<ServerViewModel>(e.Server));
			};

			// FiltersViewModel.Filters.PublishFilter();
		}

		public FiltersViewModel FiltersViewModel { get; set; }
		public ListViewModel ListViewModel { get; set; }

		public ListCollectionView FilteredServers { get; set; }
		public ObservableCollection<ServerViewModel> Servers { get; private set; } = new();
		//public ObservableCollectionProxy<ServerViewModel, Server> Servers { get; private set; }

		private int m_processedServers;
		public int ProcessedServers
		{
			get => m_processedServers;
			set => SetValue(ref m_processedServers, value);
		}

		private int m_totalServers;
		public int TotalServers
		{
			get => m_totalServers;
			set => SetValue(ref m_totalServers, value);
		}

		private bool m_canRefresh = true;
		public bool CanRefresh
		{
			get => m_canRefresh;
			set => SetValue(ref m_canRefresh, value);
		}

		public bool IsRunning => !CanRefresh;

		private async Task RefreshAllAsync()
		{
			//Servers.Clear();

			try
			{
				CanRefresh = false;
				await m_serverList.RefreshAllAsync(m_cancellationToken);
				//await foreach (Server server in m_serverList.DiscoverAsync(m_cancellationToken))
				//{
				//	Servers.Add(new ServerViewModel(server, m_cancellationToken));
				//}
			}
			finally
			{
				CanRefresh = true;
			}
		}

		public void SetServers(IList<ServerListInfo> servers)
		{
			m_serverList.SetServers(servers);
			RefreshAll();
		}

		public void RefreshAll() => RefreshAllAsync();
	}
}
