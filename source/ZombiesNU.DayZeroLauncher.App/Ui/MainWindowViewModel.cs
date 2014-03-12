using System.Collections.ObjectModel;
using System.Linq;
using Caliburn.Micro;
using zombiesnu.DayZeroLauncher.App.Core;
using zombiesnu.DayZeroLauncher.App.Ui.Favorites;
using zombiesnu.DayZeroLauncher.App.Ui.Friends;
using zombiesnu.DayZeroLauncher.App.Ui.Recent;
using zombiesnu.DayZeroLauncher.App.Ui.ServerList;

namespace zombiesnu.DayZeroLauncher.App.Ui
{
	public class MainWindowViewModel : ViewModelBase, 
		IHandle<RepublishFriendsRequest>
	{
		public MainWindowViewModel()
		{
			Tabs = new ObservableCollection<ViewModelBase>(new ViewModelBase[]
			                                               	{
			                                               		ServerListViewModel = new ServerListViewModel()
			                                               	});
			CurrentTab = Tabs.First();

			ServerList = new Core.ServerList();
			Launcher = new GameLauncher();

			SettingsViewModel = new	SettingsViewModel();
			UpdatesViewModel = new UpdatesViewModel(Launcher);
			UpdatesViewModel.LocatorChanged += (sender, e) => 
				{ 
					ServerList.GetAndUpdateAll(); 
				};

			PluginsViewModel = new PluginsViewModel();
			Launcher.ModDetailsChanged += (sender, e) =>
				{
					var modDetails = (MetaModDetails)e.UserState;
					if (modDetails != null && e.Cancelled == false && e.Error == null)
					{
						Execute.OnUiThread(() => PluginsViewModel.Refresh(modDetails.Plugins));
					}
				};

			ServerListViewModel.Launcher = Launcher;				 
			UpdatesViewModel.CheckForUpdates();
		}

		private Core.ServerList _serverList;
		public Core.ServerList ServerList
		{
			get { return _serverList; }
			set
			{
				_serverList = value;
				PropertyHasChanged("ServerList");
			}
		}

		private GameLauncher _launcher = null;
		public GameLauncher Launcher
		{
			get { return _launcher; }
			set
			{
				_launcher = value;
				PropertyHasChanged("Launcher");
			}
		}

		public ServerListViewModel ServerListViewModel { get; set; }
		public SettingsViewModel SettingsViewModel { get; set; }
		public UpdatesViewModel UpdatesViewModel { get; set; }
		public PluginsViewModel PluginsViewModel { get; set; }

		private ViewModelBase _currentTab;	
		public ViewModelBase CurrentTab
		{
			get { return _currentTab; }
			set
			{
				if(_currentTab != null)
					_currentTab.IsSelected = false;
				_currentTab = value;
				if(_currentTab != null)
					_currentTab.IsSelected = true;
				PropertyHasChanged("CurrentTab", "IsServerListSelected");
			}
		}
		public bool IsServerListSelected
		{
			get { return CurrentTab == ServerListViewModel; }
		}

		private ObservableCollection<ViewModelBase> _tabs;
		public ObservableCollection<ViewModelBase> Tabs
		{
			get { return _tabs; }
			set
			{
				_tabs = value;
				PropertyHasChanged("Tabs");
			}
		}

		public void Handle(RepublishFriendsRequest message)
		{
			foreach(var server in ServerList.Items)
			{
				App.Events.Publish(new PlayersChangedEvent(server.Players, server.Players));
			}
		}

		public void ShowSettings()
		{
			SettingsViewModel.IsVisible = true;
			UpdatesViewModel.IsVisible = false;
			PluginsViewModel.IsVisible = false;
		}

		public void ShowUpdates()
		{
			SettingsViewModel.IsVisible = false;
			UpdatesViewModel.IsVisible = true;
			PluginsViewModel.IsVisible = false;
		}
		public void ShowPlugins()
		{
			SettingsViewModel.IsVisible = false;
			UpdatesViewModel.IsVisible = false;
			PluginsViewModel.IsVisible = true;
		}

		public void Escape()
		{
			SettingsViewModel.IsVisible = false;
			UpdatesViewModel.IsVisible = false;
			PluginsViewModel.IsVisible = false;
		}
	}
}