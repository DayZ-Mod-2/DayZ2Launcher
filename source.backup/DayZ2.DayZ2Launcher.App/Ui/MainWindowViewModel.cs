using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DayZ2.DayZ2Launcher.App.Core;
using DayZ2.DayZ2Launcher.App.Ui.ServerList;

namespace DayZ2.DayZ2Launcher.App.Ui
{
	public class MainWindowViewModel : ViewModelBase
	{
		private ViewModelBase _currentTab;
		private GameLauncher_old _launcher;
		private Core.ServerList _serverList;
		private ObservableCollection<ViewModelBase> _tabs;

		public MainWindowViewModel()
		{
			Tabs = new ObservableCollection<ViewModelBase>(new ViewModelBase[]
			{
				ServerListViewModel = new ServerListViewModel()
			});
			CurrentTab = Tabs.First();

			ServerList = new Core.ServerList();
			Launcher = new GameLauncher_old();

			SettingsViewModel = new SettingsViewModel();
			UpdatesViewModel = new UpdatesViewModel(Launcher);

			SettingsViewModel.TorrentSettingsChanged += (sender, args) =>
			{
				UpdatesViewModel.ReconfigureTorrentEngine();
			};

			ServerListViewModel.Launcher = Launcher;
		}


		public Core.ServerList ServerList
		{
			get => _serverList;
			set
			{
				_serverList = value;
				PropertyHasChanged(nameof(ServerList));
			}
		}

		public GameLauncher_old Launcher
		{
			get => _launcher;
			set
			{
				_launcher = value;
				PropertyHasChanged(nameof(Launcher));
			}
		}

		public ServerListViewModel ServerListViewModel { get; set; }
		public SettingsViewModel SettingsViewModel { get; set; }
		public UpdatesViewModel UpdatesViewModel { get; set; }

		public ViewModelBase CurrentTab
		{
			get { return _currentTab; }
			set
			{
				if (_currentTab != null)
					_currentTab.IsSelected = false;
				_currentTab = value;
				if (_currentTab != null)
					_currentTab.IsSelected = true;
				PropertyHasChanged(nameof(CurrentTab), nameof(IsServerListSelected));
			}
		}

		public bool IsServerListSelected => CurrentTab == ServerListViewModel;

		public ObservableCollection<ViewModelBase> Tabs
		{
			get => _tabs;
			set
			{
				_tabs = value;
				PropertyHasChanged(nameof(Tabs));
			}
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

		public void ShowPlugins()
		{
			SettingsViewModel.IsVisible = false;
			UpdatesViewModel.IsVisible = false;
		}

		public void Escape()
		{
			SettingsViewModel.IsVisible = false;
			UpdatesViewModel.IsVisible = false;
		}
	}
}
