using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using Caliburn.Micro;
using zombiesnu.DayZeroLauncher.App.Core;

namespace zombiesnu.DayZeroLauncher.App.Ui.Recent
{
	public class RecentViewModel : ViewModelBase,
		IHandle<RecentAdded>,
		IHandle<ServerUpdated>
	{
		private readonly Dictionary<string, List<RecentServer>> _serverDictionary =
			new Dictionary<string, List<RecentServer>>();

		private readonly Timer _updateTimeTimer;
		private ObservableCollection<RecentServer> _servers;

		public RecentViewModel()
		{
			Title = "recent";

			Servers = new ObservableCollection<RecentServer>();

			/*	foreach(var recent in UserSettings.Current.RecentServers)
			{
				recent.CreateServer();
				AddToDictionary(recent);
			}
            */
			_updateTimeTimer = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
			_updateTimeTimer.Elapsed += UpdateTimeTimerOnElapsed;
			_updateTimeTimer.Start();

			UpdateServersByDateViewModel();
		}

		public ObservableCollection<RecentServer> Servers
		{
			get { return _servers; }
			set
			{
				_servers = value;
				PropertyHasChanged("Servers");
			}
		}

		public void Handle(RecentAdded message)
		{
			AddToDictionary(message.Recent);
			UpdateServersByDateViewModel();
		}

		public void Handle(ServerUpdated message)
		{
			string key = GetKey(message.Server);
			if (_serverDictionary.ContainsKey(key))
			{
				if (message.IsRemoved)
					_serverDictionary.Remove(key);
				else
				{
					foreach (RecentServer recent in _serverDictionary[key])
					{
						recent.Server = message.Server;
					}
				}
			}
		}

		private void UpdateTimeTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
		{
			_updateTimeTimer.Stop();
			Execute.OnUiThread(() =>
			{
				for (int i = UserSettings.Current.RecentServers.Count - 1; i >= 0; i--)
				{
					UserSettings.Current.RecentServers[i].RefreshAgo();
				}
			});

			_updateTimeTimer.Start();
		}

		private void AddToDictionary(RecentServer recent)
		{
			string key = GetKey(recent.Server);
			if (!_serverDictionary.ContainsKey(key))
			{
				_serverDictionary.Add(key, new List<RecentServer>());
			}
			_serverDictionary[key].Add(recent);
		}

		private void UpdateServersByDateViewModel()
		{
			Servers.Clear();
			IOrderedEnumerable<RecentServer> servers = UserSettings.Current.RecentServers
				.OrderByDescending(s => s.On);

			Servers = new ObservableCollection<RecentServer>(servers);
		}

		private string GetKey(Server server)
		{
			return server.Id;
		}
	}
}