using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;
using zombiesnu.DayZeroLauncher.App.Ui.Friends;
using zombiesnu.DayZeroLauncher.App.Ui.Recent;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	[DataContract]
	public class UserSettings
	{
		private static UserSettings _current;
        private static ServerBatchRefresher refresher = new ServerBatchRefresher();

		[DataMember] private List<string> _friends = new List<string>();
		[DataMember] private Filter _filter = new Filter();
		[DataMember] private WindowSettings _windowSettings = null; //This is null on purpose so the MainWindow view can set defaults if needed
		[DataMember] private GameOptions _gameOptions = new GameOptions();
		[DataMember] private TorrentOptions _torrentOptions = new TorrentOptions();
		[DataMember] private AppOptions _appOptions = new AppOptions();
		[DataMember] private List<string> _enabledPlugins = new List<string>();
		[DataMember] private List<FavoriteServer> _favorites = new List<FavoriteServer>();
		[DataMember] private List<RecentServer> _recentServers = new List<RecentServer>();
        [DataMember] private bool _hideUS = false; 
        [DataMember] private bool _hideEU = false;
        [DataMember] private bool _hideAU = false;

        public bool IncludeUS
        {
            get { return !_hideUS; }
            set 
			{ 
				refresher.RefreshAll();
				_hideUS = !value;
            }
        }

        public bool IncludeEU
        {
            get { return !_hideEU; }
            set
            {
                refresher.RefreshAll();
                _hideEU = !value;
            }
        }

        public bool IncludeAU
        {
            get { return !_hideAU; }
            set 
			{ 
				refresher.RefreshAll();
				_hideAU = !value;
            }
        }

		public List<string> Friends
		{
			get
			{
				if(_friends == null)
					_friends = new List<string>();

				return _friends;
			}
			set { _friends = value; }
		}

		public Filter Filter
		{
			get
			{
				if(_filter == null)
					_filter = new Filter();

				return _filter;
			}
			set { _filter = value; }
		}

		public WindowSettings WindowSettings
		{
			get { return _windowSettings; }
			set { _windowSettings = value; }
		}

		public GameOptions GameOptions
		{
			get
			{
				if(_gameOptions == null)
					_gameOptions = new GameOptions();

				return _gameOptions;
			}
			set { _gameOptions = value; }
		}

		public TorrentOptions TorrentOptions
		{
			get
			{
				if (_torrentOptions == null)
					_torrentOptions = new TorrentOptions();

				return _torrentOptions;
			}
			set { _torrentOptions = value; }
		}

		public AppOptions AppOptions
		{
			get
			{
				if(_appOptions == null)
					_appOptions = new AppOptions();

				return _appOptions;
			}
			set { _appOptions = value; }
		}

		public List<FavoriteServer> Favorites
		{
			get
			{
				if(_favorites == null)
				{
					_favorites = new List<FavoriteServer>();
				}
				return _favorites;
			}
			set { _favorites = value; }
		}

		public List<string> EnabledPlugins
		{
			get
			{
				if (_enabledPlugins == null)
				{
					_enabledPlugins = new List<string>();
				}
				return _enabledPlugins;
			}
			set { _enabledPlugins = value; }
		}

		public List<RecentServer> RecentServers
		{
			get
			{
				if(_recentServers == null)
				{
					_recentServers = new List<RecentServer>();
				}
				return _recentServers;
			}
			set { _recentServers = value; }
		}

		public void Save()
		{
			try
			{
				lock(_fileLock)
				{
					using (var fs = GetSettingsFileStream(FileMode.Create))
					{
						var serializer = new DataContractSerializer(GetType());
						serializer.WriteObject(fs, this);
						fs.Flush(true);
					}
				}
			}
			catch(Exception) {}
		}

		private static UserSettings Load()
		{
			try
			{
				using(var fs = GetSettingsFileStream(FileMode.Open))
				{
					using(var reader = new StreamReader(fs))
					{
						var rawXml = reader.ReadToEnd();
						if(string.IsNullOrWhiteSpace(rawXml))
							return new UserSettings();
						else
							return LoadFromXml(XDocument.Parse(rawXml));
					}
				}
			}
			catch(FileNotFoundException)
			{
				return new UserSettings();
			}
		}

		private static FileStream GetSettingsFileStream(FileMode fileMode)
		{
			return new FileStream(SettingsPath, fileMode);		
		}

		private static object _fileLock = new object();
		public static UserSettings Current
		{
			get
			{
				lock(_fileLock)
				{
					if (_current == null)
					{
						try { _current = Load(); }
						catch (Exception) { _current = new UserSettings(); }
					}
				}
				return _current;
			}
		}

        private static string _localDataPath;
        private static string LocalDataPath
        {
            get
            {
                if (_localDataPath == null)
                {
                    var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var zeroAppDataDir = new DirectoryInfo(Path.Combine(appDataFolder, "DayZeroLauncher"));
                    if (!zeroAppDataDir.Exists)
                        zeroAppDataDir.Create();

                    _localDataPath = zeroAppDataDir.FullName;
                }

                return _localDataPath;
            }
        }

		private static string _torrentJunkPath;
		public static string TorrentJunkPath
		{
			get
			{
				if (_torrentJunkPath == null)
				{
					var torrentJunkPathLocation = Path.Combine(LocalDataPath, "torrent");
					var dirInfo = new DirectoryInfo(torrentJunkPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_torrentJunkPath = dirInfo.FullName;
				}
				return _torrentJunkPath;
			}
		}

		private static string _contentPath;
		public static string ContentPath
		{
			get
			{
				if (_contentPath == null)
				{
					var contentPathLocation = Path.Combine(LocalDataPath, "content");
					var dirInfo = new DirectoryInfo(contentPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_contentPath = dirInfo.FullName;
				}
				return _contentPath;
			}
		}

		private static string _contentMetaPath;
		public static string ContentMetaPath
		{
			get
			{
				if (_contentMetaPath == null)
				{
					var contentMetaPathLocation = Path.Combine(ContentPath, "meta");
					var dirInfo = new DirectoryInfo(contentMetaPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_contentMetaPath = dirInfo.FullName;
				}
				return _contentMetaPath;
			}
		}

		private static string _contentDataPath;
		public static string ContentDataPath
		{
			get
			{
				if (_contentDataPath == null)
				{
					var contentDataPathLocation = Path.Combine(ContentPath, "data");
					var dirInfo = new DirectoryInfo(contentDataPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_contentDataPath = dirInfo.FullName;
				}
				return _contentDataPath;
			}
		}

		public static string ContentCurrentTagFile
		{
			get { return Path.Combine(ContentPath, "current"); }			
		}

		private static string _patchesPath;
		public static string PatchesPath
		{
			get
			{
				if (_patchesPath == null)
				{
					var patchesPathLocation = Path.Combine(LocalDataPath, "patches");
					var dirInfo = new DirectoryInfo(patchesPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_patchesPath = dirInfo.FullName;
				}
				return _patchesPath;
			}
		}

		private static string _installersPath;
		public static string InstallersPath
		{
			get
			{
				if (_installersPath == null)
				{
					var installersPathLocation = Path.Combine(LocalDataPath, "installers");
					var dirInfo = new DirectoryInfo(installersPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_installersPath = dirInfo.FullName;
				}
				return _installersPath;
			}
		}

		private static string _roamingDataPath;
		private static string RoamingDataPath
		{
			get
			{
				if (_roamingDataPath == null)
				{
					var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					var zeroAppDataDir = new DirectoryInfo(Path.Combine(appDataFolder, "DayZeroLauncher"));
					if (!zeroAppDataDir.Exists)
						zeroAppDataDir.Create();

					_roamingDataPath = zeroAppDataDir.FullName;
				}

				return _roamingDataPath;
			}
		}

		private static string _notesPath;
		private static string NotesPath
		{
			get
			{
				if (_notesPath == null)
				{ 
					const string notesFolderName = "notes";
					var newNotesLocation = Path.Combine(RoamingDataPath, notesFolderName);
					if (!Directory.Exists(newNotesLocation))
					{
						var newFolder = Directory.CreateDirectory(newNotesLocation);
						//migrate old notes
						try
						{
							var oldFolder = new DirectoryInfo(LocalDataPath);
							if (oldFolder.Exists)
							{
								FileInfo[] noteFiles = oldFolder.GetFiles("Notes_*.txt");
								foreach (var noteFile in noteFiles)
								{
									try
									{
										var newName = Path.Combine(newFolder.FullName, noteFile.Name.Replace("Notes_", ""));
										File.Move(noteFile.FullName, newName);
									}
									catch (Exception) {}
								}
							}
						}
						catch (Exception) {}
					}
					_notesPath = newNotesLocation;
				}
				return _notesPath;
			}
		}

		private static string _settingsPath;
		private static string SettingsPath
		{
			get
			{
				if (_settingsPath == null)
				{
					const string settingsFileName = "settings.xml";
					var newFileLocation = Path.Combine(RoamingDataPath, settingsFileName);

					//Migrate old settings location
					try
					{
						string oldFileLocation = Path.Combine(LocalDataPath, settingsFileName);
						if (File.Exists(oldFileLocation) && !File.Exists(newFileLocation))
							File.Move(oldFileLocation, newFileLocation);
					}
					catch (Exception) {}
					_settingsPath = newFileLocation;
				}
				return _settingsPath;
			}
		}

		private static UserSettings LoadFromXml(XDocument xDocument)
		{
			var serializer = new DataContractSerializer(typeof(UserSettings));
			var parsedVal = (UserSettings)serializer.ReadObject(xDocument.CreateReader());
			if (parsedVal != null && parsedVal.TorrentOptions != null)
			{
				if (parsedVal.TorrentOptions.RandomizePort == true)
					parsedVal.TorrentOptions.ListeningPort = 0; //this calls Random internally
			}
			return parsedVal;
		}

		public bool IsFavorite(Server server)
		{
			return Favorites.Any(f => f.Matches(server));
		}

		public void AddFavorite(Server server)
		{
			if(Favorites.Any(f => f.Matches(server)))
				return;
			Favorites.Add(new FavoriteServer(server));
			App.Events.Publish(new FavoritesUpdated(server));
			Save();
		}

		public void RemoveFavorite(Server server)
		{
			var favorite = Favorites.FirstOrDefault(f => f.Matches(server));
			if(favorite == null)
				return;
			Favorites.Remove(favorite);
			App.Events.Publish(new FavoritesUpdated(server));
			Save();
		}

		public void AddRecent(Server server)
		{
			var recentServer = new RecentServer(server, DateTime.Now);
			if(RecentServers.Count > 50)
			{
				var oldest = RecentServers.OrderBy(x => x.On).FirstOrDefault();
				RecentServers.Remove(oldest);
			}
			RecentServers.Add(recentServer);
			recentServer.Server = server;
			App.Events.Publish(new RecentAdded(recentServer));
			Save();			
		}

		public string GetNotes(Server server)
		{
			var fileName = GetNoteFileName(server);
			if(!File.Exists(fileName))
				return "";
			return File.ReadAllText(fileName, Encoding.UTF8);
		}

		public void SetNotes(Server server, string text)
		{
			var fileName = GetNoteFileName(server);
			if(string.IsNullOrWhiteSpace(text))
			{
				if(File.Exists(fileName))
					File.Delete(fileName);
			}
			else
			{
				File.WriteAllText(fileName, text, Encoding.UTF8);
			}
		}

		private static string GetNoteFileName(Server server)
		{
			return Path.Combine(NotesPath, string.Format("{0}_{1}.txt", server.IpAddress.Replace(".", "_"), server.Port));
		}

		public bool HasNotes(Server server)
		{
			return File.Exists(GetNoteFileName(server));
		}
	}

	public class RecentAdded
	{
		public RecentServer Recent { get; set; }

		public RecentAdded(RecentServer recent)
		{
			Recent = recent;
		}
	}

	public class FavoritesUpdated
	{
		public Server Server { get; set; }

		public FavoritesUpdated(Server server)
		{
			Server = server;
		}
	}
}