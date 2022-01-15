using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DayZ2.DayZ2Launcher.App.Core;
using DayZ2.DayZ2Launcher.App.Ui.ServerList;

namespace DayZ2.DayZ2Launcher.App.Ui
{
	public class MainWindowViewModel : ViewModelBase
	{
		private CancellationToken m_cancellationToken;

		private ViewModelBase m_currentTab;
		private ObservableCollection<ViewModelBase> m_tabs;

		public MainWindowViewModel(IServiceProvider services, AppCancellation cancellation)
		{
			m_cancellationToken = cancellation.Token;

			GameLauncher = new GameLauncher();

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

		public ServerListViewModel ServerListViewModel { get; set; }
		public SettingsViewModel SettingsViewModel { get; set; }
		public UpdatesViewModel UpdatesViewModel { get; set; }
		public GameLauncher GameLauncher { get; set; }

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

		public void Shutdown()
		{
			//TODO: cancel in App
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
	}
}
