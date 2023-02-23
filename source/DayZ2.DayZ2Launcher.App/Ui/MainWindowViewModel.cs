using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DayZ2.DayZ2Launcher.App.Core;
using DayZ2.DayZ2Launcher.App.Ui.ServerList;

namespace DayZ2.DayZ2Launcher.App.Ui
{
	public class MainWindowViewModel : ViewModelBase
	{
		ViewModelBase m_currentTab;
		ObservableCollection<ViewModelBase> m_tabs;

		private readonly GameLauncher m_gameLauncher;

		public ServerListViewModel ServerListViewModel { get; private set; }
		public SettingsViewModel SettingsViewModel { get; private set; }
		public UpdatesViewModel UpdatesViewModel { get; private set; }
		public AppActions AppActions { get; private set; }

		public MainWindowViewModel(IServiceProvider services, GameLauncher gameLauncher, AppActions appActions)
		{
			m_gameLauncher = gameLauncher;
			AppActions = appActions;

			Tabs = new ObservableCollection<ViewModelBase>(new ViewModelBase[]
			{
				ServerListViewModel = services.CreateInstance<ServerListViewModel>(),
				UpdatesViewModel = services.CreateInstance<UpdatesViewModel>(),
				SettingsViewModel = services.CreateInstance<SettingsViewModel>(),
			});
			CurrentTab = Tabs.First();

			SettingsViewModel.TorrentSettingsChanged += (sender, args) =>
			{
				UpdatesViewModel.ReconfigureTorrentEngine();
			};

			UpdatesViewModel.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == nameof(UpdatesViewModel.Servers))
				{
					ServerListViewModel.SetServers(UpdatesViewModel.Servers);
				}
			};
		}

		public ViewModelBase CurrentTab
		{
			get => m_currentTab;
			set
			{
				if (m_currentTab != null)
					m_currentTab.IsSelected = false;
				m_currentTab = value;
				if (m_currentTab != null)
					m_currentTab.IsSelected = true;
				OnPropertyChanged(nameof(CurrentTab), nameof(IsServerListSelected));
			}
		}

		public bool IsServerListSelected => CurrentTab == ServerListViewModel;

		public ObservableCollection<ViewModelBase> Tabs
		{
			get => m_tabs;
			set => SetValue(ref m_tabs, value);
		}

		public void ShowSettings()
		{
			SettingsViewModel.IsVisible = true;
			UpdatesViewModel.IsVisible = false;
		}

		public void ShowUpdates()
		{
			SettingsViewModel.IsVisible = false;
			UpdatesViewModel.IsVisible = true;
		}

		public void Escape()
		{
			SettingsViewModel.IsVisible = false;
			UpdatesViewModel.IsVisible = false;
		}

		public void RefreshAll()
		{
			ServerListViewModel.RefreshAll();
		}

		public void Launch()
		{
			m_gameLauncher.LaunchGame(null);
		}
	}
}
