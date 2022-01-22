using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using DayZ2.DayZ2Launcher.App.Core;
using DayZ2.DayZ2Launcher.App.UI.ServerList;

namespace DayZ2.DayZ2Launcher.App.Ui.ServerList
{
	public class ServerListViewModel : ViewModelBase
	{
		private readonly CancellationToken m_cancellationToken;

		readonly Core.ServerList m_serverList = new();

		public ServerListViewModel(IServiceProvider services, AppCancellation cancellation)
		{
			m_cancellationToken = cancellation.Token;

			Title = "Servers";

			// FiltersViewModel = new FiltersViewModel();

			m_serverList.ServersClear += (object sender, EventArgs args) =>
			{
				Servers.Clear();
			};

			m_serverList.ServerDiscovered += (object sender, ServerDiscoveredEventArgs e) =>
			{
				Servers.Add(services.CreateInstance<ServerViewModel>(e.Server));
			};

			// FiltersViewModel.Filters.PublishFilter();
		}

		// public FiltersViewModel FiltersViewModel { get; set; }

		public ListCollectionView FilteredServers { get; set; }
		public ObservableCollection<ServerViewModel> Servers { get; private set; } = new();

		private int m_processedServers = 3;
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

		private bool m_canRefresh = false;
		public bool CanRefresh
		{
			get => m_canRefresh;
			set
			{
				m_canRefresh = value;
				OnPropertyChanged(nameof(CanRefresh), nameof(IsRunning));
			}
		}

		public bool IsRunning => !CanRefresh;

		private async void RefreshAllAsync()
		{
			try
			{
				CanRefresh = false;
				ProcessedServers = 0;
				await m_serverList.RefreshAllAsync(
					new Progress<int>(p => ProcessedServers = p),
					m_cancellationToken);
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
			TotalServers = servers.Count;
			m_serverList.SetServers(servers);
			RefreshAll();
		}

		public void RefreshAll() => RefreshAllAsync();
	}
}
