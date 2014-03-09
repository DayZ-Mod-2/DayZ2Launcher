using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Caliburn.Micro;
using zombiesnu.DayZeroLauncher.App.Ui;
using zombiesnu.DayZeroLauncher.App.Ui.Controls;
using System.Net;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class ServerList : ViewModelBase,
		IHandle<RefreshServerRequest>
	{
		private bool _downloadingServerList;
		private ObservableCollection<Server> _items;

		public ServerList()
		{
			Items = new ObservableCollection<Server>();
		}

		private ServerBatchRefresher _refreshAllBatch;
		public ServerBatchRefresher RefreshAllBatch
		{
			get { return _refreshAllBatch; }
			private set
			{
				_refreshAllBatch = value;
				PropertyHasChanged("RefreshAllBatch");
			}
		}

		public ObservableCollection<Server> Items
		{
			get { return _items; }
			private set
			{
				_items = value;
				PropertyHasChanged("Items");
			}
		}

		public bool DownloadingServerList
		{
			get { return _downloadingServerList; }
			set
			{
				_downloadingServerList = value;
				PropertyHasChanged("DownloadingServerList");
			}
		}

		public void GetAndUpdateAll()
		{
			GetAll(() => UpdateAll());
		}

		public void GetAll(Action uiThreadOnComplete)
		{
			DownloadingServerList = true;
			new Thread(() =>
				{
					var servers = GetAllSync();
					Execute.OnUiThread(() =>
						{
							Items = new ObservableCollection<Server>(servers);
							DownloadingServerList = false;
							uiThreadOnComplete();
						});

				}).Start();
		}

		public List<Server> GetAllSync()
		{
            string list = "";
			{
				string serverListUrl = "https://zombies.nu/serverlist.txt";
				var locator = CalculatedGameSettings.Current.Locator;
				if (locator != null && locator.ServerListUrl != null)
					serverListUrl = locator.ServerListUrl;

				if (!string.IsNullOrWhiteSpace(serverListUrl))
				{
					using (var wc = new WebClient())
					{
						try { list = wc.DownloadString(new Uri(serverListUrl)); }
						catch (Exception) {}
					}
				}
			}

			if (string.IsNullOrEmpty(list))
				return new List<Server>(); //Empty list.. Too bad.

            var fullList = list
                .Split('\n').Select(line =>
					{
						var serverInfo = line.Split(';');
						Server server = server = new Server("", 0, "", "");
						if (serverInfo.Count() > 4)
						{
							server = new Server(serverInfo[1], serverInfo[2].TryInt(), serverInfo[3], serverInfo[4]);
						}

						server.Settings = new SortedDictionary<string, string>
						{
							{ "hostname", serverInfo[0]}
						};

						return server;
					}).ToList();

            return fullList;
		}

		public void UpdateAll()
		{
			var batch = new ServerBatchRefresher("Refreshing all servers...", Items);
			App.Events.Publish(new RefreshServerRequest(batch));
		}

		private bool _isRunningRefreshBatch;
		public void Handle(RefreshServerRequest message)
		{
			if(_isRunningRefreshBatch)
				return;

			_isRunningRefreshBatch = true;
			App.Events.Publish(new RefreshingServersChange(true));
			RefreshAllBatch = message.Batch;
			RefreshAllBatch.RefreshAllComplete += RefreshAllBatchOnRefreshAllComplete;
			RefreshAllBatch.RefreshAll();
		}

		private void RefreshAllBatchOnRefreshAllComplete()
		{
			RefreshAllBatch.RefreshAllComplete -= RefreshAllBatchOnRefreshAllComplete;
			_isRunningRefreshBatch = false;
			Execute.OnUiThread(() => { App.Events.Publish(new RefreshingServersChange(false)); });
		}
	}

	public class RefreshingServersChange
	{
		public bool IsRunning { get; set; }

		public RefreshingServersChange(bool isRunning)
		{
			IsRunning = isRunning;
		}
	}
}